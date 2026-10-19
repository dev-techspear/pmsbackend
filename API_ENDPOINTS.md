# PMS .NET API — Endpoint Reference

Extracted from the controllers in `PMS.API/Controllers/` on branch `mssql`, as of
2026-10-05. All 14 modules from the root overview's domain list now have a controller.
Ordered by lifecycle: authenticate → users/teams/masters → projects → everything that
hangs off a project.

Base route prefix for every controller: `api/v1`. All responses are wrapped in the
`ApiResponse<T>` envelope (`is_success`, `data`, `message`, `errors`). JSON is snake_case
on the wire (both directions — request bodies must be snake_case too), matching the
source, not this team's generic `api-design.md` camelCase convention (a deliberate,
flagged decision — see `MODULE_STATUS.md`).

## Legend
✅ Implemented and tested this session · 🚧 partial — see the note

---

## 1. Authentication — `AuthController` (`api/v1/auth`) ✅
`POST /auth/redirect` (Microsoft SSO callback, AllowAnonymous) · `GET /auth/me` (Bearer) ·
`POST /auth/refresh` (AllowAnonymous).
**External users (email + password)** — `users.is_external = true`, `Freelancer` role:
`POST /auth/register` (AllowAnonymous; creates the user with status `Pending`, an admin must set
status `Active` via `PUT /users/{id}` before they can sign in) · `POST /auth/login`
(AllowAnonymous; external users only). Both are rate-limited per client IP (10/min).
Internal staff stay SSO-only. No password reset / email verification yet.

**Access control** (`PMS.API/Security/AccessScopeFilter.cs`): for every authenticated request the user's role
permissions are loaded. *External users* are denied any endpoint their role does not grant (`proj-*`, `task-*`,
`issue-*`, `milestone-*`, `time-*`, `team-*`; everything else is closed except `/auth/*`, read-only `/masters/*`
and `GET /users/me`). View levels filter data for everyone who has them set: **Owned** = records they own or are
assigned, **Associated** = those plus everything in projects they own/manage/lead/belong to (time logs and
timesheets are always the caller's own). `All`, Admin, and internal users with no level set are unfiltered.
Creating inside a project the caller can't see returns 403. Tests: `PMS.Tests`.

## 1b. Project chat — `ChatController` (`api/v1`) + SignalR hub `/hubs/chat`
One room per project; members = owner, manager, delivery head, project members; admins can enter any room.
External users additionally need the role permissions `chat-view` (read) / `chat-post` (write).

- **Messages:** `GET /projects/{id}/chat/messages?beforeId=&limit=` (oldest-first page) · `POST …/chat/messages`
  (JSON `{ body }`, or `multipart/form-data` with `body` + up to 5 `files`) · `PUT …/chat/messages/{messageId}`
  (own only) · `DELETE …/chat/messages/{messageId}` (own; admins any; soft delete) · `POST …/chat/read`
  `{ last_message_id }` · `GET …/chat/members` (who can be @mentioned) · `GET /chat/unread`
  (`count` + `mention_count` per room). Posting/editing is limited to 30/min per user; messages ≤ 4000 characters.
- **Mentions:** written as `@[Display Name](userId)`. The server re-validates every token on save: only people
  really in the room count (name replaced by their current one); anything else becomes plain `@Name`.
  Unread `mention_count` comes from the validated `chat_mentions` table, so it cannot be forged.
- **Files:** `GET …/chat/attachments/{attachmentId}` (members only; sent as an attachment with
  `nosniff` + `no-store`). Allowed: png, jpg, jpeg, gif, webp, pdf, txt, csv, md, doc(x), xls(x), ppt(x), zip;
  ≤ 10 MB each, ≤ 5 per message; the first bytes must match the extension and the stored MIME type comes from
  the extension, not the client. Stored in `chat_attachments` (so they follow the room's access rules).
  Deleting a message makes its files unreachable. No virus scanning.
- **Real time:** call `POST /chat/ticket` for a **one-minute, hub-only** token, connect to `/hubs/chat` with it
  (`?access_token=` for WebSockets - the only place a token may appear in a URL; real access tokens and refresh
  tokens are refused there, and a ticket is refused everywhere else), then `JoinProject(projectId)`. The server
  pushes `MessageReceived`, `MessageEdited`, `MessageDeleted` and `AccessRevoked` (sent when you are removed
  from the project, the project is deleted, or your role loses chat permission - you stop receiving at once).
  Writes always go through REST first.
- **Needs** migrations `AddProjectChat` and `AddChatMentionsAndAttachments`
  (`dotnet ef database update -p PMS.Infrastructure -s PMS.API`). Azure App Service: enable
  **Configuration → General settings → Web sockets** (the client falls back to long-polling if it is off); on
  IIS / Windows App Service raise `maxAllowedContentLength` to at least 62914560 bytes, otherwise uploads over
  ~30 MB are refused before they reach the app.

## 2. Users — `UsersController` (`api/v1/users`) ✅
`GET /users/me` · `GET /users/search` · `POST /users` · `POST /users/sync` (mocked) ·
`GET /users` (list/filter) · `GET /users/{id}` · `PUT /users/{id}` · `DELETE /users/{id}`
(hard delete).

## 3. Teams — `TeamsController` (`api/v1/teams`) ✅
`POST /teams` · `GET /teams` · `GET /teams/search` · `GET /teams/{id}` ·
`PUT /teams/{id}` · `DELETE /teams/{id}` ·
`POST`/`DELETE /teams/{id}/members/{userEmail}`.

## 4. Master/Lookup Data — `MastersController` (`api/v1/masters`) ✅ — anonymous, no `[Authorize]`
Lookups by category, user-statuses, statuses, priorities, roles (incl. CRUD +
bulk-permissions), skills — all with `/search` variants. Seeded on every startup via
`PMS.Infrastructure/Data/DbInitializer.cs`.

## 5. Projects — `ProjectsController` (`api/v1/projects`) ✅
`POST /projects` · `GET /projects/check-name` · `GET /projects/check-sync-id` ·
`GET /projects/search` · `GET /projects` (list/filter) · `GET /projects/{id}` ·
`PUT /projects/{id}` · `DELETE /projects/{id}` (soft) · `PATCH /projects/{id}/archive` ·
`PATCH /projects/{id}/unarchive` · `GET/POST/DELETE /projects/{id}/members[/{userId}]`.
**Not ported** (advanced, deferred): export, `/dashboard`, `/audit` sub-resource, zoho
`/sync`, `clone-to-template`, and `/{id}/tasks|issues|timelogs|milestones` convenience
sub-listings (use the top-level endpoints below with `?projectId=` instead).

## 6. Tasks — `TasksController` (`api/v1/tasks`) ✅
`POST /tasks` · `GET /tasks/search` · `GET /tasks` (filter by `projectId`/`taskListId`/
`statusId`/`assigneeId`) · `GET /tasks/{id}` · `PUT /tasks/{id}` · `DELETE /tasks/{id}`
(soft). Owners and assignees are separate many-to-many sets, both settable via
`owner_ids`/`assignee_ids`. **Not ported**: bulk-create, export, parent/sub-task hierarchy
(no `ParentId` on the destination entity at all — source has it, this doesn't yet).

## 7. Task Lists — `TaskListsController` (`api/v1/task-lists`) ✅
`POST /task-lists` · `GET /task-lists?projectId=` · `GET/{id}` · `PUT/{id}` ·
`DELETE/{id}` (soft). `task_count` in the response reflects live attached tasks.

## 8. Issues — `IssuesController` (`api/v1/issues`) ✅
`POST /issues` · `GET /issues/search` · `GET /issues` (filter by `projectId`/`statusId`/
`assigneeId`/`severityId`) · `GET/{id}` · `PUT/{id}` · `DELETE/{id}` (soft, also stamps
`last_closed_time`). Followers and assignees are separate many-to-many sets.

## 9. Milestones — `MilestonesController` (`api/v1/milestones`) ✅
`POST /milestones` · `GET /milestones/search` · `GET /milestones` (filter by
`projectId`/`statusId`) · `GET/{id}` · `PUT/{id}` · `DELETE/{id}` (soft).

## 10. Time Tracking — `TimeLogsController` + `TimesheetsController` ✅
`api/v1/timelogs`: `POST` · `GET` (filter by `projectId`/`taskId`/`userId`) · `GET/{id}` ·
`PUT/{id}` · `DELETE/{id}` (soft). **Every write recalculates
`Task.CachedTimelogTotal`** for the affected task — ported from the source's SQLAlchemy
event listener, verified live (two logs, 3.5h + 1.5h, rolled the task total to 5.0 after
each write).
`api/v1/timesheets`: `POST` · `GET?projectId=&userEmail=` · `GET/{id}` · `PUT/{id}` ·
`DELETE/{id}` (soft).

## 11. Documents — `DocumentsController` (`api/v1/documents`) ✅ — **metadata only**
`POST` · `GET?projectId=` · `GET/{id}` · `PUT/{id}` · `DELETE/{id}` (soft). **No Azure
Blob Storage integration** — `file_url` is accepted as a plain string from the request
body; nothing is actually uploaded or stored by this API yet.

## 12. Project Groups — `ProjectGroupsController` (`api/v1/project-groups`) ✅
Full CRUD. Kept for completeness — confirmed dead in the source app too (the
`projects.group_id` FK that once linked here was added then dropped; see
`SOURCE_DATABASE_SCHEMA.md`). Nothing links a `Project` to a `ProjectGroup` today, and
that's correct, not a bug to fix.

## 13. Project Templates — `ProjectTemplatesController` (`api/v1/project-templates`) ✅
`POST` · `GET` (paged) · `GET/{id}` (with nested tasks) · `PUT/{id}` · `DELETE/{id}`
(soft) · `POST/{id}/tasks` · `DELETE/{id}/tasks/{taskId}`. `ProjectTemplate`/
`TemplateTask` entities didn't exist in `PMS.Domain` before this session — added along
with a dedicated migration.

## 14. Audit — `AuditController` (`api/v1/audit`) ✅ — **read-only, matches source**
`GET /audit?skip=&limit=&tableName=`. The 4 audit entities (`AuditFieldsMapping`,
`AuditLogs`, `AuditLogDetails`, `AuditMetaDataInfo`) didn't exist before this session —
added with their source PascalCase table names preserved (the only tables exempted from
the lowercase-snake_case convention everything else uses). **Nothing writes an audit row
yet** — the read endpoint is real, but there's no interceptor/middleware producing audit
entries on create/update/delete across the other controllers. That's the actual remaining
gap for this module, not the endpoint itself.

---

## Observations worth a decision, not just a note

1. **No health/readiness endpoint.** Still true — would have caught the original DB
   connectivity gap without a real client call.
2. **`/users/sync` is mocked.** Returns a canned 202, no real Microsoft Graph call.
3. **`/masters/*` has zero auth** while every other controller requires a Bearer token —
   confirm this is intentional before shipping.
4. **JSON casing is snake_case**, contradicting this team's generic `api-design.md`
   camelCase rule — deliberate, not fixed, see `MODULE_STATUS.md`.
5. **Audit doesn't audit anything yet.** The module is "built" in the sense that the read
   API works; the actual auditing behavior (writing a row on every create/update/delete
   across every other controller) doesn't exist. Scoping that is a real design decision —
   global `SaveChanges` interceptor vs. per-service logging — not a quick addition.
