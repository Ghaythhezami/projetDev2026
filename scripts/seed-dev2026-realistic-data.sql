USE [dev2026DB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @OwnerId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @ScrumMasterId uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @DeveloperId uniqueidentifier = '33333333-3333-3333-3333-333333333333';
DECLARE @QaId uniqueidentifier = '44444444-4444-4444-4444-444444444444';

DECLARE @ProjectId uniqueidentifier = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @Sprint1Id uniqueidentifier = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1';
DECLARE @Sprint2Id uniqueidentifier = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2';
DECLARE @EpicAuthId uniqueidentifier = 'cccccccc-cccc-cccc-cccc-ccccccccccc1';
DECLARE @EpicPlanningId uniqueidentifier = 'cccccccc-cccc-cccc-cccc-ccccccccccc2';
DECLARE @EpicAnalyticsId uniqueidentifier = 'cccccccc-cccc-cccc-cccc-ccccccccccc3';

DECLARE @StoryLoginId uniqueidentifier = 'dddddddd-dddd-dddd-dddd-ddddddddddd1';
DECLARE @StoryBacklogId uniqueidentifier = 'dddddddd-dddd-dddd-dddd-ddddddddddd2';
DECLARE @StorySprintId uniqueidentifier = 'dddddddd-dddd-dddd-dddd-ddddddddddd3';
DECLARE @StoryVelocityId uniqueidentifier = 'dddddddd-dddd-dddd-dddd-ddddddddddd4';
DECLARE @StoryKanbanId uniqueidentifier = 'dddddddd-dddd-dddd-dddd-ddddddddddd5';

DECLARE @IssueLoginApiId uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1';
DECLARE @IssueTokenId uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2';
DECLARE @IssueBacklogId uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3';
DECLARE @IssueSprintBoardId uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee4';
DECLARE @IssueVelocityId uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee5';
DECLARE @IssueKanbanId uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee6';

INSERT INTO dbo.Users
    (UserId, Nom, Prenom, Email, MotDePasse, Telephone, Role, RefreshToken, RefreshTokenExpiryTime, Token, isDeleted)
SELECT @OwnerId, 'Ben Salem', 'Amira', 'amira.bensalem@poulina.com', 'Dev2026@123', '+216 20 451 120', 'ProductOwner', NULL, DATEADD(day, 30, SYSUTCDATETIME()), NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @OwnerId);

INSERT INTO dbo.Users
    (UserId, Nom, Prenom, Email, MotDePasse, Telephone, Role, RefreshToken, RefreshTokenExpiryTime, Token, isDeleted)
SELECT @ScrumMasterId, 'Mansouri', 'Youssef', 'youssef.mansouri@poulina.com', 'Dev2026@123', '+216 29 883 014', 'ScrumMaster', NULL, DATEADD(day, 30, SYSUTCDATETIME()), NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @ScrumMasterId);

INSERT INTO dbo.Users
    (UserId, Nom, Prenom, Email, MotDePasse, Telephone, Role, RefreshToken, RefreshTokenExpiryTime, Token, isDeleted)
SELECT @DeveloperId, 'Karray', 'Nour', 'nour.karray@poulina.com', 'Dev2026@123', '+216 55 762 331', 'Developer', NULL, DATEADD(day, 30, SYSUTCDATETIME()), NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @DeveloperId);

INSERT INTO dbo.Users
    (UserId, Nom, Prenom, Email, MotDePasse, Telephone, Role, RefreshToken, RefreshTokenExpiryTime, Token, isDeleted)
SELECT @QaId, 'Trabelsi', 'Mehdi', 'mehdi.trabelsi@poulina.com', 'Dev2026@123', '+216 24 118 909', 'QA', NULL, DATEADD(day, 30, SYSUTCDATETIME()), NULL, 0
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @QaId);

IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE [Key] = 'TRC')
BEGIN
    INSERT INTO dbo.Projects
        (ProjectId, ProjectName, ProjectDescription, [Key], OwnerId, CreatedAt, UpdatedAt, IsFinished, FinishedAt, TotalCompletedPoints, isDeleted)
    VALUES
        (@ProjectId, 'Trace Server Agile Platform',
         'Internal project for planning, execution, kanban tracking, AI-assisted prioritization, and sprint analytics.',
         'TRC', @OwnerId, DATEADD(day, -45, SYSUTCDATETIME()), SYSUTCDATETIME(), 0, NULL, 21, 0);

    INSERT INTO dbo.ProjectMembers
        (ProjectMemberId, ProjectId, MemberId, isDeleted)
    VALUES
        (NEWID(), @ProjectId, @OwnerId, 0),
        (NEWID(), @ProjectId, @ScrumMasterId, 0),
        (NEWID(), @ProjectId, @DeveloperId, 0),
        (NEWID(), @ProjectId, @QaId, 0);

    INSERT INTO dbo.KanbanColumns
        (ColumnId, ProjectId, Status, WipLimit, isDeleted)
    VALUES
        (NEWID(), @ProjectId, 1, 12, 0),
        (NEWID(), @ProjectId, 2, 5, 0),
        (NEWID(), @ProjectId, 3, 4, 0),
        (NEWID(), @ProjectId, 4, 10, 0);

    INSERT INTO dbo.Sprints
        (SprintId, Name, StartDate, EndDate, Status, ProjectId, CompletedPoints, isDeleted)
    VALUES
        (@Sprint1Id, 'Sprint 1 - Authentication and Project Setup', DATEADD(day, -28, SYSUTCDATETIME()), DATEADD(day, -15, SYSUTCDATETIME()), 5, @ProjectId, 21, 0),
        (@Sprint2Id, 'Sprint 2 - Planning and Execution Board', DATEADD(day, -14, SYSUTCDATETIME()), DATEADD(day, 1, SYSUTCDATETIME()), 2, @ProjectId, 8, 0);

    INSERT INTO dbo.Epics
        (EpicId, Title, Description, ProjectId, isDeleted)
    VALUES
        (@EpicAuthId, 'Secure User Access', 'Authentication, roles, and session handling for agile project users.', @ProjectId, 0),
        (@EpicPlanningId, 'Agile Planning Workspace', 'Epics, stories, sprint planning, acceptance criteria, and backlog grooming.', @ProjectId, 0),
        (@EpicAnalyticsId, 'AI and Delivery Insights', 'Prediction logs, priority suggestions, velocity tracking, and progress reporting.', @ProjectId, 0);

    INSERT INTO dbo.UserStories
        (UserStoryId, Title, Description, StoryPoints, Priority, MoSCoW, EpicId, SprintId, Status, isDeleted)
    VALUES
        (@StoryLoginId, 'As a user, I can sign in securely',
         'Users authenticate with email and password so they can access assigned projects and protected API endpoints.',
         8, 4, 1, @EpicAuthId, @Sprint1Id, 4, 0),
        (@StoryBacklogId, 'As a product owner, I can organize the product backlog',
         'The product owner can create user stories, rank them, and add acceptance criteria before sprint planning.',
         13, 3, 1, @EpicPlanningId, @Sprint2Id, 2, 0),
        (@StorySprintId, 'As a scrum master, I can plan sprint scope',
         'The scrum master can move stories into a sprint and monitor committed story points against capacity.',
         8, 3, 2, @EpicPlanningId, @Sprint2Id, 2, 0),
        (@StoryVelocityId, 'As a project lead, I can see delivery velocity',
         'The project lead can review completed points and predicted velocity to plan upcoming sprint capacity.',
         5, 2, 2, @EpicAnalyticsId, @Sprint2Id, 1, 0),
        (@StoryKanbanId, 'As a developer, I can move work through kanban columns',
         'Developers can track issues from todo through review and done while respecting WIP limits.',
         5, 2, 3, @EpicPlanningId, NULL, 1, 0);

    INSERT INTO dbo.AcceptanceCriteria
        (CriterionId, Description, IsSatisfied, UserStoryId, isDeleted)
    VALUES
        (NEWID(), 'Valid credentials return an access token and refresh token.', 1, @StoryLoginId, 0),
        (NEWID(), 'Invalid credentials return a clear unauthorized response.', 1, @StoryLoginId, 0),
        (NEWID(), 'A story can be created with title, description, points, priority, and MoSCoW value.', 0, @StoryBacklogId, 0),
        (NEWID(), 'Stories assigned to a sprint appear in the planning board.', 0, @StorySprintId, 0),
        (NEWID(), 'Velocity prediction uses previous closed sprints when available.', 0, @StoryVelocityId, 0),
        (NEWID(), 'WIP limits are visible for todo, in progress, review, and done columns.', 0, @StoryKanbanId, 0);

    INSERT INTO dbo.Issues
        (IssueId, Title, Status, [Order], UserStoryId, AssigneeId, isDeleted)
    VALUES
        (@IssueLoginApiId, 'Implement login endpoint and JWT generation', 4, 1, @StoryLoginId, @DeveloperId, 0),
        (@IssueTokenId, 'Add refresh token expiry handling', 4, 2, @StoryLoginId, @DeveloperId, 0),
        (@IssueBacklogId, 'Create API flow for backlog story creation', 2, 1, @StoryBacklogId, @DeveloperId, 0),
        (@IssueSprintBoardId, 'Support assigning user stories to active sprint', 2, 2, @StorySprintId, @ScrumMasterId, 0),
        (@IssueVelocityId, 'Generate velocity prediction log from closed sprints', 1, 3, @StoryVelocityId, @DeveloperId, 0),
        (@IssueKanbanId, 'Expose kanban column WIP data for project board', 1, 4, @StoryKanbanId, @QaId, 0);

    INSERT INTO dbo.SubTasks
        (SubTaskId, Title, IsCompleted, IssueId, isDeleted)
    VALUES
        (NEWID(), 'Validate request model for login', 1, @IssueLoginApiId, 0),
        (NEWID(), 'Map authenticated user to response DTO', 1, @IssueLoginApiId, 0),
        (NEWID(), 'Persist refresh token expiry date', 1, @IssueTokenId, 0),
        (NEWID(), 'Add story creation validation', 0, @IssueBacklogId, 0),
        (NEWID(), 'Return sprint capacity summary', 0, @IssueSprintBoardId, 0),
        (NEWID(), 'Calculate average completed points', 0, @IssueVelocityId, 0);

    INSERT INTO dbo.Comments
        (CommentId, Content, CreatedAt, UpdatedAt, IssueId, AuthorId, isDeleted)
    VALUES
        (NEWID(), 'Login endpoint tested successfully with the seeded product owner account.', DATEADD(day, -20, SYSUTCDATETIME()), NULL, @IssueLoginApiId, @QaId, 0),
        (NEWID(), 'Please keep backlog validation aligned with the UserStory enum values.', DATEADD(day, -3, SYSUTCDATETIME()), NULL, @IssueBacklogId, @ScrumMasterId, 0),
        (NEWID(), 'Velocity prediction should ignore active sprints and use closed sprints only.', DATEADD(day, -1, SYSUTCDATETIME()), NULL, @IssueVelocityId, @OwnerId, 0);

    INSERT INTO dbo.Attachments
        (AttachmentId, FileName, FileType, FileSize, BlobUrl, IssueId, UploaderId, isDeleted)
    VALUES
        (NEWID(), 'auth-flow-notes.pdf', 'application/pdf', 248320, 'https://storage.local/dev2026/auth-flow-notes.pdf', @IssueLoginApiId, @ScrumMasterId, 0),
        (NEWID(), 'planning-board-wireframe.png', 'image/png', 531240, 'https://storage.local/dev2026/planning-board-wireframe.png', @IssueSprintBoardId, @OwnerId, 0);

    INSERT INTO dbo.ScrumCeremonies
        (CeremonyId, [Date], Type, Notes, Obstacles, SprintId)
    VALUES
        (NEWID(), DATEADD(day, -13, SYSUTCDATETIME()), 'Planning', 'Sprint goal confirmed: deliver backlog creation and planning board basics.', 'Need final agreement on WIP limits.', @Sprint2Id),
        (NEWID(), DATEADD(day, -7, SYSUTCDATETIME()), 'Daily Scrum', 'Backlog API is in progress; sprint assignment endpoint is next.', 'Velocity prediction depends on closed sprint data quality.', @Sprint2Id),
        (NEWID(), DATEADD(day, 1, SYSUTCDATETIME()), 'Review', 'Demo planning flow, kanban columns, and initial AI prediction logs.', 'None reported yet.', @Sprint2Id);

    INSERT INTO dbo.AIPredictionLogs
        (PredictionId, PredictionType, InputData, PredictedValue, ConfidenceScore, CreatedAt, isDeleted)
    VALUES
        (NEWID(), 'Priority', 'Story: organize product backlog; points: 13; MoSCoW: Must', 'High', 0.86, DATEADD(day, -5, SYSUTCDATETIME()), 0),
        (NEWID(), 'Assignment', 'Issue: Generate velocity prediction log from closed sprints; skill: backend analytics', 'nour.karray@poulina.com', 0.78, DATEADD(day, -2, SYSUTCDATETIME()), 0),
        (NEWID(), 'Velocity', 'Closed sprint points: 21; active sprint committed points: 31', 'Recommended capacity: 24 points', 0.72, SYSUTCDATETIME(), 0);

    INSERT INTO dbo.Notifications
        (NotificationId, Message, IsRead, CreatedAt, Link, ReceiverId)
    VALUES
        (NEWID(), 'You were assigned to: Create API flow for backlog story creation.', 0, DATEADD(day, -3, SYSUTCDATETIME()), '/projects/TRC/issues', @DeveloperId),
        (NEWID(), 'Sprint 2 review is scheduled for tomorrow.', 0, SYSUTCDATETIME(), '/projects/TRC/sprints', @OwnerId),
        (NEWID(), 'Velocity prediction is ready for review.', 1, DATEADD(hour, -6, SYSUTCDATETIME()), '/projects/TRC/analytics', @ScrumMasterId);
END

COMMIT TRANSACTION;

SELECT
    'Seed completed for project TRC. If rows already existed, the script skipped inserting duplicates.' AS Result;
GO
