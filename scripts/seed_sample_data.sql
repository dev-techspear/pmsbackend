-- DEV-ONLY sample data for the PMS tables in the local techspear DB (schema [pms]).
-- Run AFTER `dotnet ef database update` and one app start (DbInitializer seeds roles/lookups/skills).
-- Idempotent: every insert is guarded by a natural-key check. Never run against production.
-- Login for all sample users: <email> / DevPassword#1  (external/password accounts)
--   sqlcmd -S "(localdb)\Techspear" -E -C -I -d techspear -b -i scripts/seed_sample_data.sql
SET NOCOUNT ON; SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @pw nvarchar(255) = N'PBKDF2-SHA256$600000$HCfRDp3Jjl1dDCEwhnuYRQ==$NzD7nnJQlPR9nAwkVzhqXToe9kpV7OeTFqWqoa1tVV8=';
DECLARE @active int = (SELECT Id FROM pms.user_statuses WHERE Name='Active');

-- Users ---------------------------------------------------------------------
DECLARE @u TABLE (Pub nvarchar(50), Emp nvarchar(50), Fn nvarchar(100), Ln nvarchar(100), Email nvarchar(255), Uname nvarchar(100), Job nvarchar(100), Role nvarchar(50));
INSERT @u VALUES
 ('USR-1001','EMP001','Asha','Menon','asha.menon@techspear.dev','asha.menon','Engineering Manager','Admin'),
 ('USR-1002','EMP002','Ravi','Kumar','ravi.kumar@techspear.dev','ravi.kumar','Senior Developer','Team Lead'),
 ('USR-1003','EMP003','Meera','Nair','meera.nair@techspear.dev','meera.nair','Project Manager','Project Manager'),
 ('USR-1004','EMP004','Karthik','S','karthik.s@techspear.dev','karthik.s','Full-Stack Developer','Employee'),
 ('USR-1005','EMP005','Divya','R','divya.r@techspear.dev','divya.r','UI/UX Designer','Employee');
INSERT pms.users (PublicId,EmployeeId,FirstName,LastName,Email,Username,JobTitle,JoinDate,PasswordHash,DisplayName,Country,Language,Timezone,IsSynced,IsExternal,RoleId,StatusId,CreatedAt,IsActive,IsDeleted)
SELECT x.Pub,x.Emp,x.Fn,x.Ln,x.Email,x.Uname,x.Job,'2025-01-15',@pw,x.Fn+' '+x.Ln,'India','English','Asia/Kolkata',0,1,r.Id,@active,SYSUTCDATETIME(),1,0
FROM @u x JOIN pms.roles r ON r.Name=x.Role
WHERE NOT EXISTS (SELECT 1 FROM pms.users WHERE Email=x.Email);

-- Teams ---------------------------------------------------------------------
INSERT pms.teams (PublicId,Name,TeamEmail,BudgetAllocation,Description,TeamType,MaxTeamSize,LeadEmail,CreatedAt,IsActive,IsDeleted)
SELECT v.a,v.b,v.c,v.d,v.e,v.f,v.g,v.h,SYSUTCDATETIME(),1,0 FROM (VALUES
 ('TEAM-1001','Platform Engineering','platform@techspear.dev',250000.00,'Backend and infrastructure','Engineering',10,'ravi.kumar@techspear.dev'),
 ('TEAM-1002','Product Design','design@techspear.dev',90000.00,'UX and visual design','Design',6,'divya.r@techspear.dev')
) v(a,b,c,d,e,f,g,h) WHERE NOT EXISTS (SELECT 1 FROM pms.teams t WHERE t.TeamEmail=v.c);

INSERT pms.user_team_link (MembersId,TeamsId)
SELECT u.Id,t.Id FROM (VALUES ('ravi.kumar@techspear.dev','platform@techspear.dev'),('karthik.s@techspear.dev','platform@techspear.dev'),
 ('asha.menon@techspear.dev','platform@techspear.dev'),('divya.r@techspear.dev','design@techspear.dev'),('meera.nair@techspear.dev','design@techspear.dev')) m(ue,te)
JOIN pms.users u ON u.Email=m.ue JOIN pms.teams t ON t.TeamEmail=m.te
WHERE NOT EXISTS (SELECT 1 FROM pms.user_team_link l WHERE l.MembersId=u.Id AND l.TeamsId=t.Id);

-- Projects ------------------------------------------------------------------
DECLARE @pm int=(SELECT Id FROM pms.users WHERE Email='meera.nair@techspear.dev');
DECLARE @owner int=(SELECT Id FROM pms.users WHERE Email='asha.menon@techspear.dev');
INSERT pms.projects (ProjectIdSync,PublicId,AccountName,ProjectName,CustomerName,ClientName,BillingModel,ProjectType,ExpectedStartDate,ExpectedEndDate,OwnerId,ProjectManagerId,StatusId,PriorityId,Description,EstimatedHours,ActualHours,IsArchived,IsTemplate,IsGroup,IsProcessed,CreatedAt,IsActive,IsDeleted)
SELECT v.sync,v.pub,v.acct,v.pname,v.cust,v.cli,v.bm,v.pt,v.s,v.e,@owner,@pm,
 (SELECT Id FROM pms.master_lookups WHERE Category='ProjectStatus' AND Value=v.st),
 (SELECT Id FROM pms.master_lookups WHERE Category='TaskPriority' AND Value=v.pr),
 v.d,v.eh,v.ah,0,0,0,0,SYSUTCDATETIME(),1,0
FROM (VALUES
 ('SYNC-1001','PRJ-1001','Techspear','Customer Portal Revamp','Acme Corp','Acme','T&M','External','2026-09-01','2026-12-31','In Progress','High','Redesign and rebuild the customer portal.',800.0,310.5),
 ('SYNC-1002','PRJ-1002','Techspear','Internal HR Tooling','Techspear','','Non-Billable','Internal','2026-10-01','2027-01-31','Planning','Medium','Internal HR workflow automation.',300.0,0.0)
) v(sync,pub,acct,pname,cust,cli,bm,pt,s,e,st,pr,d,eh,ah)
WHERE NOT EXISTS (SELECT 1 FROM pms.projects p WHERE p.ProjectName=v.pname);

INSERT pms.project_members (ProjectId,UserId,ProjectProfile,PortalProfile,RoleInProject,IsOwner,IsProcessed,CreatedAt)
SELECT p.Id,u.Id,m.prof,'Employee',m.rl,m.own,0,SYSUTCDATETIME()
FROM (VALUES ('Customer Portal Revamp','meera.nair@techspear.dev','Project Lead','Project Manager',1),
 ('Customer Portal Revamp','ravi.kumar@techspear.dev','Developer','Tech Lead',0),
 ('Customer Portal Revamp','karthik.s@techspear.dev','Developer','Developer',0),
 ('Customer Portal Revamp','divya.r@techspear.dev','Member','Designer',0),
 ('Internal HR Tooling','meera.nair@techspear.dev','Project Lead','Project Manager',1),
 ('Internal HR Tooling','karthik.s@techspear.dev','Developer','Developer',0)) m(pn,ue,prof,rl,own)
JOIN pms.projects p ON p.ProjectName=m.pn JOIN pms.users u ON u.Email=m.ue
WHERE NOT EXISTS (SELECT 1 FROM pms.project_members x WHERE x.ProjectId=p.Id AND x.UserId=u.Id);

-- Milestones ----------------------------------------------------------------
INSERT pms.milestones (PublicId,MilestoneName,Description,ProjectId,OwnerId,StatusId,PriorityId,StartDate,EndDate,CompletionPercentage,IsProcessed,CreatedAt,IsActive,IsDeleted)
SELECT v.pub,v.n,v.d,p.Id,@pm,(SELECT Id FROM pms.master_lookups WHERE Category='MilestoneStatus' AND Value=v.st),
 (SELECT Id FROM pms.master_lookups WHERE Category='TaskPriority' AND Value='High'),v.s,v.e,v.pc,0,SYSUTCDATETIME(),1,0
FROM (VALUES ('MS-1001','Design Sign-off','UX designs approved','Customer Portal Revamp','Completed','2026-09-01','2026-09-30',100),
 ('MS-1002','Beta Release','Beta to pilot customers','Customer Portal Revamp','In Progress','2026-10-01','2026-11-30',45),
 ('MS-1003','Requirements Freeze','HR tooling scope agreed','Internal HR Tooling','Planning','2026-10-01','2026-11-15',20)) v(pub,n,d,pn,st,s,e,pc)
JOIN pms.projects p ON p.ProjectName=v.pn
WHERE NOT EXISTS (SELECT 1 FROM pms.milestones m WHERE m.PublicId=v.pub);

-- Task lists ----------------------------------------------------------------
INSERT pms.task_lists (ProjectId,MilestoneId,Name,Description,CreatedAt,IsActive,IsDeleted)
SELECT p.Id,m.Id,v.n,v.d,SYSUTCDATETIME(),1,0
FROM (VALUES ('Customer Portal Revamp','MS-1001','Design','UI/UX tasks'),('Customer Portal Revamp','MS-1002','Frontend','React work'),
 ('Customer Portal Revamp','MS-1002','Backend','API work'),('Internal HR Tooling','MS-1003','Discovery','Requirements')) v(pn,ms,n,d)
JOIN pms.projects p ON p.ProjectName=v.pn JOIN pms.milestones m ON m.PublicId=v.ms
WHERE NOT EXISTS (SELECT 1 FROM pms.task_lists t WHERE t.ProjectId=p.Id AND t.Name=v.n);

-- Tasks ---------------------------------------------------------------------
INSERT pms.tasks (PublicId,TaskName,Description,ProjectId,TaskListId,MilestoneId,AssigneeId,CreatedById,OwnerId,StatusId,PriorityId,StartDate,DueDate,CompletionPercentage,EstimatedHours,WorkHours,CachedTimelogTotal,BillingType,IsProcessed,CreatedAt,IsActive,IsDeleted)
SELECT v.pub,v.n,v.d,p.Id,tl.Id,tl.MilestoneId,u.Id,@pm,@pm,
 (SELECT Id FROM pms.master_lookups WHERE Category='TaskStatus' AND Value=v.st),
 (SELECT Id FROM pms.master_lookups WHERE Category='TaskPriority' AND Value=v.pr),
 v.s,v.e,v.pc,v.eh,v.wh,v.wh,'Billable',0,SYSUTCDATETIME(),1,0
FROM (VALUES
 ('TSK-1001','Design system & wireframes','Create component library and wireframes','Customer Portal Revamp','Design','divya.r@techspear.dev','Completed','High','2026-09-01','2026-09-25',100,60.0,58.0),
 ('TSK-1002','Build login & dashboard UI','React login and dashboard screens','Customer Portal Revamp','Frontend','karthik.s@techspear.dev','In Progress','High','2026-10-05','2026-11-20',60,80.0,44.0),
 ('TSK-1003','Orders API','REST endpoints for orders','Customer Portal Revamp','Backend','ravi.kumar@techspear.dev','In Progress','Critical','2026-10-05','2026-11-25',40,70.0,26.0),
 ('TSK-1004','Notification service','Email/SMS notifications','Customer Portal Revamp','Backend','ravi.kumar@techspear.dev','Open','Medium','2026-11-15','2026-12-10',0,40.0,0.0),
 ('TSK-1005','Gather HR workflow requirements','Interview HR stakeholders','Internal HR Tooling','Discovery','meera.nair@techspear.dev','In Progress','Medium','2026-10-05','2026-11-10',50,20.0,9.0)
) v(pub,n,d,pn,tln,ue,st,pr,s,e,pc,eh,wh)
JOIN pms.projects p ON p.ProjectName=v.pn JOIN pms.task_lists tl ON tl.ProjectId=p.Id AND tl.Name=v.tln JOIN pms.users u ON u.Email=v.ue
WHERE NOT EXISTS (SELECT 1 FROM pms.tasks t WHERE t.PublicId=v.pub);

INSERT pms.task_assignees (AssigneesId,Task1Id) SELECT t.AssigneeId,t.Id FROM pms.tasks t
WHERE t.PublicId LIKE 'TSK-100_' AND t.AssigneeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pms.task_assignees a WHERE a.Task1Id=t.Id AND a.AssigneesId=t.AssigneeId);
INSERT pms.task_owners (OwnersId,TaskId) SELECT t.OwnerId,t.Id FROM pms.tasks t
WHERE t.PublicId LIKE 'TSK-100_' AND t.OwnerId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM pms.task_owners a WHERE a.TaskId=t.Id AND a.OwnersId=t.OwnerId);

-- Issues --------------------------------------------------------------------
INSERT pms.issues (PublicId,BugName,Description,ProjectId,MilestoneId,ReporterId,AssigneeId,StatusId,PriorityId,SeverityId,ClassificationId,Module,ReproducibleFlag,StartDate,DueDate,EstimatedHours,IsProcessed,CreatedAt,IsActive,IsDeleted)
SELECT v.pub,v.n,v.d,p.Id,m.Id,@pm,u.Id,
 (SELECT Id FROM pms.master_lookups WHERE Category='IssueStatus' AND Value=v.st),
 (SELECT Id FROM pms.master_lookups WHERE Category='TaskPriority' AND Value=v.pr),
 (SELECT Id FROM pms.master_lookups WHERE Category='IssueSeverity' AND Value=v.sev),
 (SELECT Id FROM pms.master_lookups WHERE Category='IssueClassification' AND Value=v.cl),
 v.modl,1,'2026-11-01','2026-11-20',v.eh,0,SYSUTCDATETIME(),1,0
FROM (VALUES
 ('BUG-1001','Login fails on Safari','Session cookie rejected on Safari 17','Customer Portal Revamp','MS-1002','karthik.s@techspear.dev','Open','High','High','Other Bugs','Auth',6.0),
 ('BUG-1002','Slow order list query','Orders list takes >5s with 10k rows','Customer Portal Revamp','MS-1002','ravi.kumar@techspear.dev','In Progress','Medium','Medium','Performance','Orders',10.0)
) v(pub,n,d,pn,ms,ue,st,pr,sev,cl,modl,eh)
JOIN pms.projects p ON p.ProjectName=v.pn JOIN pms.milestones m ON m.PublicId=v.ms JOIN pms.users u ON u.Email=v.ue
WHERE NOT EXISTS (SELECT 1 FROM pms.issues i WHERE i.PublicId=v.pub);

-- Time logs -----------------------------------------------------------------
INSERT pms.timelogs (PublicId,LogTitle,UserId,CreatedById,ProjectId,TaskId,Date,DailyLogHours,Notes,BillingType,GeneralLog,IsProcessed,CreatedAt,IsActive,IsDeleted)
SELECT v.pub,v.n,u.Id,u.Id,t.ProjectId,t.Id,v.dt,v.h,v.notes,'Billable',0,0,SYSUTCDATETIME(),1,0
FROM (VALUES ('TL-1001','UI work','karthik.s@techspear.dev','TSK-1002','2026-11-10',6.0,'Dashboard layout'),
 ('TL-1002','API work','ravi.kumar@techspear.dev','TSK-1003','2026-11-10',7.5,'Orders endpoints'),
 ('TL-1003','UI work','karthik.s@techspear.dev','TSK-1002','2026-11-11',5.5,'Login form')) v(pub,n,ue,tp,dt,h,notes)
JOIN pms.users u ON u.Email=v.ue JOIN pms.tasks t ON t.PublicId=v.tp
WHERE NOT EXISTS (SELECT 1 FROM pms.timelogs x WHERE x.PublicId=v.pub);

COMMIT;
SELECT 'users' t,COUNT(*) n FROM pms.users UNION ALL SELECT 'teams',COUNT(*) FROM pms.teams UNION ALL SELECT 'projects',COUNT(*) FROM pms.projects
UNION ALL SELECT 'milestones',COUNT(*) FROM pms.milestones UNION ALL SELECT 'task_lists',COUNT(*) FROM pms.task_lists UNION ALL SELECT 'tasks',COUNT(*) FROM pms.tasks
UNION ALL SELECT 'issues',COUNT(*) FROM pms.issues UNION ALL SELECT 'timelogs',COUNT(*) FROM pms.timelogs;
