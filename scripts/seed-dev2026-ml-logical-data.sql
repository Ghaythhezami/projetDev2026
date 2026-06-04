-- PostgreSQL seed data for the DEV2026 / TraceServer ML demo.
-- Run this in pgAdmin while connected to your PostgreSQL dev2026 database.

BEGIN;

CREATE TEMP TABLE seed_should_insert AS
SELECT NOT EXISTS (
    SELECT 1
    FROM "Projects"
    WHERE "Key" = 'MLTRC'
) AS value;

CREATE OR REPLACE FUNCTION pg_temp.seed_uuid(seed text)
RETURNS uuid
LANGUAGE sql
AS $$
    SELECT (
        substr(md5(seed), 1, 8) || '-' ||
        substr(md5(seed), 9, 4) || '-' ||
        substr(md5(seed), 13, 4) || '-' ||
        substr(md5(seed), 17, 4) || '-' ||
        substr(md5(seed), 21, 12)
    )::uuid;
$$;

CREATE TEMP TABLE seed_ids
(
    name text PRIMARY KEY,
    id uuid NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_ids (name, id)
VALUES
    ('ProductOwner', '10000000-0000-0000-0000-000000000001'),
    ('ScrumMaster', '10000000-0000-0000-0000-000000000002'),
    ('BackendDev', '10000000-0000-0000-0000-000000000003'),
    ('FrontendDev', '10000000-0000-0000-0000-000000000004'),
    ('QA', '10000000-0000-0000-0000-000000000005'),
    ('AIDev', '10000000-0000-0000-0000-000000000006'),
    ('Project', '20000000-0000-0000-0000-000000000001'),
    ('EpicAuth', '30000000-0000-0000-0000-000000000001'),
    ('EpicPlanning', '30000000-0000-0000-0000-000000000002'),
    ('EpicExecution', '30000000-0000-0000-0000-000000000003'),
    ('EpicAnalytics', '30000000-0000-0000-0000-000000000004');

INSERT INTO "Users"
    ("UserId", "Nom", "Prenom", "Email", "MotDePasse", "Telephone", "Role", "RefreshToken", "RefreshTokenExpiryTime", "Token", "isDeleted")
SELECT v."UserId", v."Nom", v."Prenom", v."Email", 'Dev2026@123', v."Telephone", v."Role", NULL, CURRENT_TIMESTAMP + INTERVAL '30 days', NULL, false
FROM (
    VALUES
        ((SELECT id FROM seed_ids WHERE name = 'ProductOwner'), 'Ben Salem', 'Amira', 'amira.bensalem@poulina.com', '+216 20 451 120', 'ProductOwner'),
        ((SELECT id FROM seed_ids WHERE name = 'ScrumMaster'), 'Mansouri', 'Youssef', 'youssef.mansouri@poulina.com', '+216 29 883 014', 'ScrumMaster'),
        ((SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Karray', 'Nour', 'nour.karray@poulina.com', '+216 55 762 331', 'BackendDeveloper'),
        ((SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Jaziri', 'Lina', 'lina.jaziri@poulina.com', '+216 52 440 882', 'FrontendDeveloper'),
        ((SELECT id FROM seed_ids WHERE name = 'QA'), 'Trabelsi', 'Mehdi', 'mehdi.trabelsi@poulina.com', '+216 24 118 909', 'QA'),
        ((SELECT id FROM seed_ids WHERE name = 'AIDev'), 'Gharbi', 'Sami', 'sami.gharbi@poulina.com', '+216 56 610 772', 'AIDeveloper')
) AS v("UserId", "Nom", "Prenom", "Email", "Telephone", "Role")
ON CONFLICT ("UserId") DO NOTHING;

INSERT INTO "Projects"
    ("ProjectId", "ProjectName", "ProjectDescription", "Key", "OwnerId", "CreatedAt", "UpdatedAt", "IsFinished", "FinishedAt", "TotalCompletedPoints", "isDeleted")
SELECT
    (SELECT id FROM seed_ids WHERE name = 'Project'),
    'Trace Server ML Training Project',
    'Coherent agile dataset for priority prediction, issue assignment, kanban flow, and sprint velocity demos.',
    'MLTRC',
    (SELECT id FROM seed_ids WHERE name = 'ProductOwner'),
    CURRENT_TIMESTAMP - INTERVAL '98 days',
    CURRENT_TIMESTAMP,
    false,
    NULL,
    85,
    false
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "ProjectMembers"
    ("ProjectMemberId", "ProjectId", "MemberId", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-project-member-' || v.member_id::text),
    (SELECT id FROM seed_ids WHERE name = 'Project'),
    v.member_id,
    false
FROM (
    VALUES
        ((SELECT id FROM seed_ids WHERE name = 'ProductOwner')),
        ((SELECT id FROM seed_ids WHERE name = 'ScrumMaster')),
        ((SELECT id FROM seed_ids WHERE name = 'BackendDev')),
        ((SELECT id FROM seed_ids WHERE name = 'FrontendDev')),
        ((SELECT id FROM seed_ids WHERE name = 'QA')),
        ((SELECT id FROM seed_ids WHERE name = 'AIDev'))
) AS v(member_id)
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "KanbanColumns"
    ("ColumnId", "ProjectId", "Status", "WipLimit", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-kanban-' || v.status::text),
    (SELECT id FROM seed_ids WHERE name = 'Project'),
    v.status,
    v.wip_limit,
    false
FROM (
    VALUES
        (1, 12),
        (2, 5),
        (3, 4),
        (4, 12)
) AS v(status, wip_limit)
WHERE (SELECT value FROM seed_should_insert);

CREATE TEMP TABLE seed_sprints
(
    sprint_no int PRIMARY KEY,
    sprint_id uuid NOT NULL,
    name text NOT NULL,
    start_offset int NOT NULL,
    end_offset int NOT NULL,
    status int NOT NULL,
    completed_points int NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_sprints
    (sprint_no, sprint_id, name, start_offset, end_offset, status, completed_points)
VALUES
    (1, '40000000-0000-0000-0000-000000000001', 'Sprint 1 - Secure Access Foundation', -84, -71, 5, 18),
    (2, '40000000-0000-0000-0000-000000000002', 'Sprint 2 - Project Planning Core', -70, -57, 5, 22),
    (3, '40000000-0000-0000-0000-000000000003', 'Sprint 3 - Execution Board', -56, -43, 5, 24),
    (4, '40000000-0000-0000-0000-000000000004', 'Sprint 4 - Reporting and Quality', -42, -29, 5, 21),
    (5, '40000000-0000-0000-0000-000000000005', 'Sprint 5 - AI Assisted Delivery', -14, -1, 2, 10),
    (6, '40000000-0000-0000-0000-000000000006', 'Sprint 6 - Forecasting Hardening', 0, 13, 1, 0);

INSERT INTO "Sprints"
    ("SprintId", "Name", "StartDate", "EndDate", "Status", "ProjectId", "CompletedPoints", "isDeleted")
SELECT
    sprint_id,
    name,
    CURRENT_TIMESTAMP + (start_offset || ' days')::interval,
    CURRENT_TIMESTAMP + (end_offset || ' days')::interval,
    status,
    (SELECT id FROM seed_ids WHERE name = 'Project'),
    completed_points,
    false
FROM seed_sprints
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "Epics"
    ("EpicId", "Title", "Description", "ProjectId", "isDeleted")
SELECT v.epic_id, v.title, v.description, (SELECT id FROM seed_ids WHERE name = 'Project'), false
FROM (
    VALUES
        ((SELECT id FROM seed_ids WHERE name = 'EpicAuth'), 'Secure Access and Permissions', 'Authentication, authorization, account tokens, and permission boundaries.'),
        ((SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 'Backlog and Sprint Planning', 'Product backlog, epics, user stories, estimation, sprint planning, and acceptance criteria.'),
        ((SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 'Kanban Execution Workflow', 'Issues, subtasks, comments, attachments, WIP limits, and execution board flow.'),
        ((SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 'AI Delivery Insights', 'Priority prediction, assignment recommendation, velocity analytics, and prediction logs.')
) AS v(epic_id, title, description)
WHERE (SELECT value FROM seed_should_insert);

CREATE TEMP TABLE seed_stories
(
    story_no int PRIMARY KEY,
    story_id uuid NOT NULL,
    title text NOT NULL,
    description text NOT NULL,
    story_points int NOT NULL,
    priority int NOT NULL,
    moscow int NOT NULL,
    epic_id uuid NOT NULL,
    sprint_no int NULL,
    status int NOT NULL,
    assignee_id uuid NULL,
    work_type text NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_stories
    (story_no, story_id, title, description, story_points, priority, moscow, epic_id, sprint_no, status, assignee_id, work_type)
VALUES
    (1, '50000000-0000-0000-0000-000000000001', 'Sign in with JWT access tokens', 'Implement secure login, JWT access token creation, and refresh-token expiry validation for protected API calls.', 8, 4, 1, (SELECT id FROM seed_ids WHERE name = 'EpicAuth'), 1, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (2, '50000000-0000-0000-0000-000000000002', 'Restrict project access to members only', 'Reject project reads and updates when the authenticated user is not the owner or a project member.', 5, 4, 1, (SELECT id FROM seed_ids WHERE name = 'EpicAuth'), 1, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (3, '50000000-0000-0000-0000-000000000003', 'Return user profile after login', 'Return normalized user identity, role, and display name so the frontend can build the session header.', 3, 2, 2, (SELECT id FROM seed_ids WHERE name = 'EpicAuth'), 1, 4, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (4, '50000000-0000-0000-0000-000000000004', 'Create project with unique key', 'Validate and create a project with a unique short key so teams can link sprints, epics, and members correctly.', 5, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 2, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (5, '50000000-0000-0000-0000-000000000005', 'Manage project membership', 'Add and list project members with stable roles to support assignment and authorization workflows.', 5, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 2, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (6, '50000000-0000-0000-0000-000000000006', 'Create epics and user stories', 'Product owner can create epics and user stories with title, description, points, priority, and MoSCoW value.', 8, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 2, 4, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (7, '50000000-0000-0000-0000-000000000007', 'Add acceptance criteria to stories', 'Attach testable acceptance criteria to each user story before the story can be considered ready for sprint planning.', 3, 2, 2, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 2, 4, (SELECT id FROM seed_ids WHERE name = 'QA'), 'QA'),
    (8, '50000000-0000-0000-0000-000000000008', 'Create sprint and assign stories', 'Scrum master can create a sprint, set dates, and assign ready stories while keeping planned points visible.', 8, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 2, 4, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (9, '50000000-0000-0000-0000-000000000009', 'Move issues across kanban statuses', 'Developers can move issues through todo, in progress, review, and done while preserving board order.', 8, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 3, 4, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (10, '50000000-0000-0000-0000-000000000010', 'Enforce WIP limits on active columns', 'Warn users when in-progress or review columns exceed configured WIP limits for the project.', 5, 3, 2, (SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 3, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (11, '50000000-0000-0000-0000-000000000011', 'Add subtasks to issues', 'Break an issue into subtasks and calculate completion from checked subtasks for execution tracking.', 5, 2, 2, (SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 3, 4, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (12, '50000000-0000-0000-0000-000000000012', 'Comment on issue progress', 'Team members can comment on issues with creation dates so reviewers can follow decisions and blockers.', 3, 2, 2, (SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 3, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (13, '50000000-0000-0000-0000-000000000013', 'Attach evidence files to issues', 'QA can attach screenshots or PDF notes to issues to document defects and acceptance validation.', 3, 2, 3, (SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 3, 4, (SELECT id FROM seed_ids WHERE name = 'QA'), 'QA'),
    (14, '50000000-0000-0000-0000-000000000014', 'Show sprint completed points', 'Display completed story points per closed sprint for velocity charting and release planning.', 5, 2, 2, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 4, 4, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (15, '50000000-0000-0000-0000-000000000015', 'Log AI prediction requests', 'Store prediction type, input payload, predicted value, confidence, and date for audit and model evaluation.', 8, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 4, 4, (SELECT id FROM seed_ids WHERE name = 'AIDev'), 'AI'),
    (16, '50000000-0000-0000-0000-000000000016', 'Add assignment notifications', 'Notify assignees when issues are assigned so active work is visible without refreshing the board.', 3, 2, 2, (SELECT id FROM seed_ids WHERE name = 'EpicExecution'), 4, 4, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend'),
    (17, '50000000-0000-0000-0000-000000000017', 'Test completed sprint reporting', 'Verify closed sprint totals, story statuses, and completed points before analytics are used by ML features.', 5, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 4, 4, (SELECT id FROM seed_ids WHERE name = 'QA'), 'QA'),
    (18, '50000000-0000-0000-0000-000000000018', 'Predict user story priority', 'Predict priority from story description, story points, MoSCoW value, and historical labels.', 8, 4, 1, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 5, 3, (SELECT id FROM seed_ids WHERE name = 'AIDev'), 'AI'),
    (19, '50000000-0000-0000-0000-000000000019', 'Recommend issue assignee', 'Recommend the best member based on issue text, work type, previous assignments, and active workload.', 8, 3, 1, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 5, 2, (SELECT id FROM seed_ids WHERE name = 'AIDev'), 'AI'),
    (20, '50000000-0000-0000-0000-000000000020', 'Forecast sprint velocity', 'Calculate average velocity from closed sprints and prepare a forecast for the next sprint capacity.', 5, 3, 2, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), 5, 2, (SELECT id FROM seed_ids WHERE name = 'AIDev'), 'AI'),
    (21, '50000000-0000-0000-0000-000000000021', 'Improve backlog filters', 'Filter stories by priority, MoSCoW, sprint, and status to help product owners groom the backlog faster.', 3, 2, 3, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), 5, 1, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (22, '50000000-0000-0000-0000-000000000022', 'Export sprint summary CSV', 'Export sprint name, completed points, active issues, and story count for lightweight reporting.', 2, 1, 3, (SELECT id FROM seed_ids WHERE name = 'EpicAnalytics'), NULL, 1, (SELECT id FROM seed_ids WHERE name = 'FrontendDev'), 'Frontend'),
    (23, '50000000-0000-0000-0000-000000000023', 'Archive finished project', 'Allow product owner to mark a project finished after all stories are done and reports are exported.', 3, 1, 4, (SELECT id FROM seed_ids WHERE name = 'EpicPlanning'), NULL, 1, (SELECT id FROM seed_ids WHERE name = 'BackendDev'), 'Backend');

INSERT INTO "UserStories"
    ("UserStoryId", "Title", "Description", "StoryPoints", "Priority", "MoSCoW", "EpicId", "SprintId", "Status", "isDeleted")
SELECT
    st.story_id,
    st.title,
    st.description,
    st.story_points,
    st.priority,
    st.moscow,
    st.epic_id,
    sp.sprint_id,
    st.status,
    false
FROM seed_stories st
LEFT JOIN seed_sprints sp ON sp.sprint_no = st.sprint_no
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "AcceptanceCriteria"
    ("CriterionId", "Description", "IsSatisfied", "UserStoryId", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-criterion-main-' || story_no::text),
    'Main workflow for "' || title || '" is implemented and covered by validation.',
    status = 4,
    story_id,
    false
FROM seed_stories
WHERE (SELECT value FROM seed_should_insert)
UNION ALL
SELECT
    pg_temp.seed_uuid('mltrc-criterion-edge-' || story_no::text),
    'Edge cases for "' || title || '" are reviewed before the story is marked done.',
    status = 4,
    story_id,
    false
FROM seed_stories
WHERE priority >= 3
  AND (SELECT value FROM seed_should_insert);

INSERT INTO "Issues"
    ("IssueId", "Title", "Status", "Order", "UserStoryId", "AssigneeId", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-issue-' || story_no::text),
    work_type || ' implementation - ' || title,
    CASE
        WHEN status = 4 THEN 4
        WHEN status = 3 THEN 3
        WHEN status = 2 THEN 2
        ELSE 1
    END,
    story_no,
    story_id,
    assignee_id,
    false
FROM seed_stories
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "SubTasks"
    ("SubTaskId", "Title", "IsCompleted", "IssueId", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-subtask-design-' || s.story_no::text),
    'Design solution for ' || s.title,
    s.status IN (3, 4),
    pg_temp.seed_uuid('mltrc-issue-' || s.story_no::text),
    false
FROM seed_stories s
WHERE (SELECT value FROM seed_should_insert)
UNION ALL
SELECT
    pg_temp.seed_uuid('mltrc-subtask-implement-' || s.story_no::text),
    'Implement and review ' || s.title,
    s.status = 4,
    pg_temp.seed_uuid('mltrc-issue-' || s.story_no::text),
    false
FROM seed_stories s
WHERE (SELECT value FROM seed_should_insert)
UNION ALL
SELECT
    pg_temp.seed_uuid('mltrc-subtask-validate-' || s.story_no::text),
    'Validate acceptance criteria for ' || s.title,
    s.status = 4,
    pg_temp.seed_uuid('mltrc-issue-' || s.story_no::text),
    false
FROM seed_stories s
WHERE s.priority >= 3
  AND (SELECT value FROM seed_should_insert);

INSERT INTO "Comments"
    ("CommentId", "Content", "CreatedAt", "UpdatedAt", "IssueId", "AuthorId", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-comment-' || s.story_no::text),
    CASE
        WHEN s.status = 4 THEN 'Completed and accepted during sprint review.'
        WHEN s.status = 3 THEN 'Ready for review; waiting for final QA validation.'
        WHEN s.status = 2 THEN 'Implementation started and main technical path is clear.'
        ELSE 'Ready in backlog; not started yet.'
    END,
    CURRENT_TIMESTAMP - ((s.story_no % 60) || ' days')::interval,
    NULL,
    pg_temp.seed_uuid('mltrc-issue-' || s.story_no::text),
    CASE
        WHEN s.work_type = 'QA' THEN (SELECT id FROM seed_ids WHERE name = 'QA')
        WHEN s.work_type = 'AI' THEN (SELECT id FROM seed_ids WHERE name = 'AIDev')
        ELSE (SELECT id FROM seed_ids WHERE name = 'ScrumMaster')
    END,
    false
FROM seed_stories s
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "Attachments"
    ("AttachmentId", "FileName", "FileType", "FileSize", "BlobUrl", "IssueId", "UploaderId", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-attachment-' || s.story_no::text),
    'evidence-story-' || s.story_no::text || '.png',
    'image/png',
    120000 + (s.story_no * 7310),
    'https://storage.local/dev2026/mltrc/evidence-story-' || s.story_no::text || '.png',
    pg_temp.seed_uuid('mltrc-issue-' || s.story_no::text),
    (SELECT id FROM seed_ids WHERE name = 'QA'),
    false
FROM seed_stories s
WHERE (s.work_type = 'QA' OR s.priority >= 4)
  AND (SELECT value FROM seed_should_insert);

INSERT INTO "ScrumCeremonies"
    ("CeremonyId", "Date", "Type", "Notes", "Obstacles", "SprintId")
SELECT
    pg_temp.seed_uuid('mltrc-ceremony-planning-' || sprint_no::text),
    CURRENT_TIMESTAMP + (start_offset || ' days')::interval,
    'Planning',
    name || ': planned work follows team capacity and previous velocity.',
    CASE WHEN status = 5 THEN 'Resolved during sprint.' ELSE 'AI model validation still in progress.' END,
    sprint_id
FROM seed_sprints
WHERE (SELECT value FROM seed_should_insert)
UNION ALL
SELECT
    pg_temp.seed_uuid('mltrc-ceremony-review-' || sprint_no::text),
    CURRENT_TIMESTAMP + (end_offset || ' days')::interval,
    CASE WHEN status = 5 THEN 'Review' ELSE 'Daily Scrum' END,
    CASE
        WHEN status = 5 THEN name || ': completed points recorded for velocity training.'
        ELSE name || ': active work has mixed todo, in progress, and review statuses.'
    END,
    CASE WHEN status = 5 THEN 'No open blocker.' ELSE 'Need more prediction samples before model evaluation.' END,
    sprint_id
FROM seed_sprints
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "AIPredictionLogs"
    ("PredictionId", "PredictionType", "InputData", "PredictedValue", "ConfidenceScore", "CreatedAt", "isDeleted")
SELECT
    pg_temp.seed_uuid('mltrc-prediction-priority-' || story_no::text),
    'Priority',
    jsonb_build_object('storyPoints', story_points, 'moscow', moscow, 'text', title)::text,
    CASE priority WHEN 4 THEN 'Critical' WHEN 3 THEN 'High' WHEN 2 THEN 'Medium' ELSE 'Low' END,
    CASE priority WHEN 4 THEN 0.91 WHEN 3 THEN 0.84 WHEN 2 THEN 0.78 ELSE 0.72 END,
    CURRENT_TIMESTAMP - (story_no || ' days')::interval,
    false
FROM seed_stories
WHERE sprint_no IS NOT NULL
  AND (SELECT value FROM seed_should_insert)
UNION ALL
SELECT
    pg_temp.seed_uuid('mltrc-prediction-assignment-' || story_no::text),
    'Assignment',
    jsonb_build_object('workType', work_type, 'storyPoints', story_points, 'priority', priority)::text,
    assignee_id::text,
    CASE work_type WHEN 'AI' THEN 0.89 WHEN 'Backend' THEN 0.84 WHEN 'Frontend' THEN 0.82 ELSE 0.79 END,
    CURRENT_TIMESTAMP - (story_no || ' days')::interval,
    false
FROM seed_stories
WHERE assignee_id IS NOT NULL
  AND (SELECT value FROM seed_should_insert)
UNION ALL
SELECT
    pg_temp.seed_uuid('mltrc-prediction-velocity'),
    'Velocity',
    jsonb_build_object('closedSprintPoints', ARRAY[18, 22, 24, 21], 'average', 21.25, 'activeCommittedPoints', 34)::text,
    '21.25',
    0.88,
    CURRENT_TIMESTAMP,
    false
WHERE (SELECT value FROM seed_should_insert);

INSERT INTO "Notifications"
    ("NotificationId", "Message", "IsRead", "CreatedAt", "Link", "ReceiverId")
SELECT
    pg_temp.seed_uuid('mltrc-notification-' || story_no::text),
    'You are assigned to story: ' || title,
    status = 4,
    CURRENT_TIMESTAMP - (story_no || ' days')::interval,
    '/projects/MLTRC/stories/' || story_id::text,
    assignee_id
FROM seed_stories
WHERE assignee_id IS NOT NULL
  AND status IN (1, 2, 3)
  AND (SELECT value FROM seed_should_insert);

COMMIT;

SELECT
    'ML logical PostgreSQL seed completed for project MLTRC. Closed sprint velocity average should be 21.25 points.' AS result;
