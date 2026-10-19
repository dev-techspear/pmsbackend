# Source Database Schema (extracted from `PMS-backend`)

Reverse-engineered from the SQLAlchemy models in `PMS-backend/app/models/*.py` on branch
`new_features` (the current source checkout). This is the contract `pmsbackend`'s EF Core
entities/migrations need to match. Source stack: MySQL via SQLAlchemy async; destination is
SQL Server via EF Core, so FK `ON DELETE CASCADE` chains below will hit SQL Server's
multi-cascade-path restriction (see root `PROJECT_OVERVIEW.md` §4 Stage 1.2) — flagged per
table where it applies.

**No live source database is reachable from this environment** — `PMS-backend` ships only
`.env.example` (all values blank), no real `.env`. So this is extracted from code, not a
live introspection: the ORM models (the contract the running app actually queries against)
cross-checked against the full 36-revision Alembic migration chain in
`PMS-backend/alembic/versions/` to catch drift between what a migration added and what the
current model classes expose. One drift case found and resolved (see `project_groups`
below); no others surfaced. If a live source DB ever becomes reachable, treat this document
as the thing to diff against it, not as a substitute for that check.

Every table below also carries the `AuditMixin` columns unless noted otherwise:
`created_at` (datetime, UTC, not null), `updated_at` (datetime, nullable), `is_active`
(bool, default true), `is_deleted` (bool, default false) — i.e. soft-delete is the pattern,
never a hard `DELETE`.

## Core entities

### `users`
PK `id`. `public_id`, `employee_id`, `email`, `username` all unique. `first_name`,
`last_name`, `phone`, `job_title`, `join_date`, `password_hash` (nullable — SSO users have
none), `display_name`, `gender`, `country`, `state`, `language`, `timezone`, `o365_id`
(unique, nullable), `is_synced`, `is_external`.
FKs: `role_id` → `roles.id` (SET NULL), `status_id` → `user_statuses.id` (SET NULL),
`manager_email` → `users.email` (SET NULL, **self-referencing**).

### `roles`
PK `id`. `name` (unique), `description`, `permissions` (JSON).

### `user_statuses`, `statuses`, `priorities`, `skills`
All simple lookup tables: PK `id`, `name` (unique). (`master_lookups` below has mostly
superseded `statuses`/`priorities` as a generic category table — check which one
`pmsbackend` should actually port; porting both is likely wrong.)

### `master_lookups`
PK `id`. `category`, `value`, `label`, `color`, `icon`, `order_index`, `is_active`.
Unique on `(category, value)`. This is the generic status/priority/severity/classification
lookup used by `Project`, `Task`, `Issue`, `Milestone`, `TimeLog` etc. via `*_id` FKs — the
single most-referenced table in the schema.

### `teams`
PK `id`. `public_id`, `name`, `team_email` (both unique), `budget_allocation` (decimal
12,2), `description`, `team_type`, `max_team_size`, `primary_communication_channel`,
`channel_id`. FK `lead_email` → `users.email` (SET NULL).

### `user_team_link` (join table, has its own `id` + audit columns + `is_active`/`is_deleted`)
`user_id` → `users.id` (CASCADE), `team_id` → `teams.id` (CASCADE).

### `user_skill_link` (composite PK, join table)
`user_id` → `users.id` (CASCADE), `skill_id` → `skills.id` (CASCADE).

## Project domain

### `projects`
PK `id`. `project_id_sync` (unique), `public_id` (unique), `account_name`, `project_name`
(unique), `customer_name`, `client_name`, `billing_model`, `project_type`,
`project_status_external`, `expected_start_date`, `expected_end_date`,
`actual_start_date`, `actual_end_date`, `description`, `tags`, `estimated_hours`/
`actual_hours` (decimal 10,2), `ms_teams_group_id`, `ms_teams_channel_id`, `is_archived`,
`is_template`, `is_group`, `is_processed`, `previous_status_id`.
FKs: `owner_id`, `project_manager_id`, `delivery_head_id` → `users.id` (all SET NULL);
`template_id` → `project_templates.id` (SET NULL); `status_id`, `priority_id` →
`master_lookups.id`.
**Cascade note:** `tasks`, `issues`, `milestones`, `timelogs`, `task_lists`, `documents`
all cascade-delete from `projects` — that's 6 cascade paths off one parent, each of which
also cascades further (e.g. `tasks` → `timelogs`, `sub_tasks`). On SQL Server this will
collide with the multi-path cascade restriction; use `DeleteBehavior.NoAction` + an
application-level cascading soft-delete (consistent with the `is_deleted` pattern anyway).

### `project_members` (composite PK `project_id` + `user_id`)
`project_profile`, `portal_profile`, `role_in_project`, `is_owner`, `is_processed`.
FKs: `invitation_status_id`, `previous_invitation_status_id` → `master_lookups.id`.
Has its own `created_at`/`updated_at` but **not** `is_active`/`is_deleted` (not a full
`AuditMixin` table).

### `v_project_stats` (a **view**, not a table — `task_count`, `completed_task_count`,
`issue_count`, `milestone_count` per project). Don't model this as an EF entity with
migrations; model it as a queryable view or compute it, matching how it's actually used
(read-only, `viewonly=True` on the source side).

### `tasks`
PK `id`. `public_id` (unique). `task_name` + `project_id` unique together. `description`,
`tags`, `start_date`, `due_date`, `completion_date`, `duration` (int, stored — a computed
fallback exists in code from `due_date - start_date`), `completion_percentage`,
`estimated_hours`, `work_hours`, `cached_timelog_total` (denormalized, maintained by a
SQLAlchemy `after_insert`/`after_update`/`after_delete` event listener on `timelogs` — see
below), `billing_type`, `is_processed`, `previous_status_id`.
FKs: `project_id` (CASCADE), `task_list_id` → `task_lists.id` (SET NULL), `milestone_id`
→ `milestones.id` (SET NULL), `associated_team_id` → `teams.id` (SET NULL), `parent_id` →
`tasks.id` (CASCADE, **self-referencing sub-tasks**), `assignee_id`/`created_by_id`/
`owner_id` → `users.id` (SET NULL), `status_id`/`priority_id` → `master_lookups.id`.

### `task_owners`, `task_assignees` (both composite-PK join tables, no audit columns)
`task_id` → `tasks.id` (CASCADE), `user_id` → `users.id` (CASCADE). Note **owners and
assignees are modeled as two separate many-to-many sets** on the same task — don't
collapse them into one relation when porting.

### `task_lists`
PK `id`. `name`, `description`. FKs: `project_id` (CASCADE), `milestone_id` → `milestones`
(SET NULL), `created_by_id` → `users.id` (SET NULL).

### `issues`
PK `id`. `public_id` (unique). `bug_name` + `project_id` unique together. `description`,
`module`, `tags`, `flag`, `reproducible_flag` (bool), `start_date`, `due_date`,
`last_closed_time`, `last_modified_time`, `estimated_hours`, `is_processed`,
`previous_status_id`.
FKs: `project_id` (CASCADE), `milestone_id` → `milestones` (SET NULL), `associated_team_id`
→ `teams` (SET NULL), `reporter_id`/`assignee_id` → `users.id` (SET NULL), `status_id`/
`priority_id`/`severity_id`/`classification_id` → `master_lookups.id` (**4 separate
lookup dimensions** — don't merge severity into priority).

### `issue_followers`, `issue_assignees` (composite PK, no audit columns)
Same pattern as task_owners/task_assignees — followers and assignees are distinct sets.

### `issue_document_link` (composite PK + full audit columns incl. `is_active`/`is_deleted`)
`issue_id` → `issues.id` (CASCADE), `document_id` → `documents.id` (CASCADE).

### `milestones`
PK `id`. `public_id` (unique). `milestone_name`, `description`, `flags`, `tags`,
`start_date`, `end_date`, `completion_percentage`, `is_processed`, `previous_status_id`.
FKs: `project_id` (CASCADE), `owner_id` → `users.id` (SET NULL), `status_id`/
`priority_id` → `master_lookups.id`.

### `v_milestone_stats` (view, same caveat as `v_project_stats`): `task_count`,
`completed_task_count`, `issue_count` per milestone.

### `timelogs`
PK `id`. `public_id`, `log_title`, `date` (not null), `daily_log_hours` (decimal 10,2, not
null), `time_period`, `notes`, `billing_type`, `general_log` (bool), `is_processed`,
`previous_approval_status_id`.
FKs: `user_id` → `users.id` (CASCADE, not null), `created_by_id` → `users.id` (SET NULL),
`project_id` → `projects.id` (CASCADE), `task_id` → `tasks.id` (CASCADE), `issue_id` →
`issues.id` (CASCADE), `timesheet_id` → `timesheets.id` (SET NULL), `approval_status_id` →
`master_lookups.id`.
**Behavioral logic that must be ported, not just the columns**: every insert/update/delete
recalculates and writes `tasks.cached_timelog_total` for the affected task (currently a
SQLAlchemy ORM event listener — ported as an EF Core `SaveChanges` interceptor or an
application-service responsibility, not left out).

### `timesheets`
PK `id`. `name`, `start_date`, `end_date` (not null), `billing_type`, `total_hours`
(decimal 5,2), `approval_status` (plain string here, **not** a `master_lookups` FK — unlike
almost everything else in this schema). FKs: `project_id` → `projects.id` (not null, no
explicit `ondelete` → defaults to RESTRICT), `user_email` → `users.email` (not null).

### `documents`
PK `id`. `public_id` (unique), `title`, `description`, `file_url`, `file_type`,
`file_size`. FKs: `project_id` → `projects.id` (CASCADE, not null), `uploaded_by_email` →
`users.email` (SET NULL). `file_url` points at Azure Blob Storage per the root overview.

### `project_templates`
PK `id`. `public_id`, `name` (unique, not null), `description`, `billing_type`,
`is_public`. FKs: `source_project_id` → `projects.id` (SET NULL), `created_by_id` →
`users.id` (SET NULL).

### `template_tasks`
PK `id`. `title` + `template_id` unique together. `description`, `estimated_hours`,
`duration`, `billing_type`, `tags`, `order_index`. FK: `template_id` →
`project_templates.id` (CASCADE). **Not** a full `AuditMixin` table (no `is_active`/
`is_deleted`/`created_at`/`updated_at`).

### `project_groups`
PK `id`. `name` (unique, not null), `description`. No FKs — flat grouping entity, and
**confirmed dead in the source app itself**, not just unmodeled: migration
`1df14cd0c9cd_add_zoho_backend_extensions` added `projects.group_id` → `project_groups.id`
(SET NULL), but migration `ea2828384a3a_email_as_key_refactor` dropped `projects.group_id`
again a few revisions later and nothing downstream re-added it. The table still exists and
still gets created (nothing drops `project_groups` itself), but no column anywhere
references it any more — current `Project`/`ProjectGroup` models correctly have no
relationship between them. **Don't port a `project_id` ↔ `project_group_id` link into
`pmsbackend`** — that would be adding a relationship the source doesn't actually have.

## Audit tables — already MSSQL-shaped in source

These four were written using SQL Server types (`BigInteger`, `UNIQUEIDENTIFIER`) even
though the source app runs on MySQL — i.e. someone already started this migration at the
schema level. Port these almost verbatim.

### `AuditFieldsMapping`
PK `ID` (bigint). `EntityName`, `FieldName`, `DisplayName`, `IsActive` (not null),
`IsVisible`, `OrderNo`. PascalCase table/column names (unlike everything else in this
schema, which is snake_case) — preserve as-is, don't "fix" the casing.

### `AuditLogs`
PK `ID` (bigint). `TableName`, `Action` (int), `PerformedBy` (uuid, not null),
`PerformedOn` (datetime, not null), `RecordName`, `TransactionId` (uuid, not null),
`Comments`, `ModuleName`.

### `AuditLogDetails`
PK `Id` (bigint). FK `AuditLogId` → `AuditLogs.ID` (not null). `FieldName` (not null),
`OldValue`, `NewValue`, `ValueType` (int).

### `AuditMetaDataInfo`
PK `Id` (int). `File_name`, `StartDate`/`EndDate` (not null), `ModuleOrEntityName`,
`CreatedOn` (not null, defaults to now).

---

## Cross-check against `pmsbackend/PMS.Domain/Entities/`

Already modeled as C# entities: `Document`, `Issue`, `MasterLookup`, `Milestone`,
`Project`, `ProjectGroup`, `ProjectMember`, `Role`, `Skill`, `Task`, `TaskList`, `Team`,
`TimeLog`, `Timesheet`, `User`, `UserStatus`.

**Not yet modeled on the destination side**: `ProjectTemplate` / `TemplateTask`,
`AuditFieldsMapping` / `AuditLogs` / `AuditLogDetails` / `AuditMetaDataInfo`, the plain
`statuses`/`priorities` lookup tables (may be intentional if fully superseded by
`master_lookups`), and — confirmed separately — **no EF Core migration exists anywhere in
`pmsbackend`** (`PMS.Infrastructure` has no `Migrations/` folder), so none of this is
actually in a SQL Server database yet, modeled or not.
