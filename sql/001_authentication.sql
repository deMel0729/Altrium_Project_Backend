/* ---------------------------------------------------------------------------
   Altrium CRM - schema changes required by authentication.

   Run once against altrium-crm-db. Safe to run more than once.
   Written as a single batch (no GO) so it pastes straight into the Azure portal
   Query editor, which does not support the GO separator.
   --------------------------------------------------------------------------- */

SET NOCOUNT ON;

/* 1. Clear NULL hashes first: the widened column is NOT NULL and the ALTER below
      fails outright if any row still holds a NULL. */
UPDATE dbo.[User] SET password_hash = 'NO-LOGIN' WHERE password_hash IS NULL;

/* 2. password_hash has to hold the PBKDF2 format written by PasswordHasher:
      PBKDF2$<iterations>$<base64 salt>$<base64 hash>   (about 85 characters).
      Anything narrower silently truncates the hash and nobody can ever log in. */
IF EXISTS (SELECT 1
           FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.[User]')
             AND name = 'password_hash'
             AND (CASE WHEN system_type_id IN (231, 239) THEN max_length / 2 ELSE max_length END < 200
                  OR system_type_id <> 231))
BEGIN
    ALTER TABLE dbo.[User] ALTER COLUMN password_hash NVARCHAR(200) NOT NULL;
    PRINT 'password_hash widened to NVARCHAR(200).';
END
ELSE
    PRINT 'password_hash is already wide enough.';

/* 3. Any password_hash written before authentication existed is unusable - it is
      plain text, empty, or whatever the client happened to send. Mark them so the
      intent is explicit: these accounts cannot sign in until a password is set. */
UPDATE dbo.[User]
SET password_hash = 'NO-LOGIN'
WHERE password_hash NOT LIKE 'PBKDF2$%';

/* 4. Login looks accounts up by email, so it should be unique and indexed.
      Creating the index fails if duplicates exist, so check first. */
DECLARE @dupes INT = (SELECT COUNT(*) FROM (SELECT email FROM dbo.[User] GROUP BY email HAVING COUNT(*) > 1) d);

IF @dupes > 0
    PRINT 'WARNING: ' + CAST(@dupes AS VARCHAR(10)) + ' duplicated email(s). Unique index skipped - clean them up, then re-run.';
ELSE IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_User_Email' AND object_id = OBJECT_ID('dbo.[User]'))
BEGIN
    CREATE UNIQUE INDEX UX_User_Email ON dbo.[User](email);
    PRINT 'UX_User_Email created.';
END
ELSE
    PRINT 'UX_User_Email already exists.';

/* 5. Ownership columns are read on every request now, so index them. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Company_UserId' AND object_id = OBJECT_ID('dbo.Company'))
    CREATE INDEX IX_Company_UserId ON dbo.Company(user_id) INCLUDE (is_active);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_UserId' AND object_id = OBJECT_ID('dbo.Leads'))
    CREATE INDEX IX_Leads_UserId ON dbo.Leads(user_id) INCLUDE (is_active);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Deals_UserId' AND object_id = OBJECT_ID('dbo.Deals'))
    CREATE INDEX IX_Deals_UserId ON dbo.Deals(user_id) INCLUDE (is_active);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Engagement_UserId' AND object_id = OBJECT_ID('dbo.Engagement'))
    CREATE INDEX IX_Engagement_UserId ON dbo.Engagement(user_id) INCLUDE (is_active);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FollowUps_UserId' AND object_id = OBJECT_ID('dbo.follow_ups'))
    CREATE INDEX IX_FollowUps_UserId ON dbo.follow_ups(user_id) INCLUDE (is_active);

/* 6. What you have now. Every account should read 'no password' until 002 runs. */
SELECT user_id, name, email, user_role, is_active,
       CASE WHEN password_hash LIKE 'PBKDF2$%' THEN 'can sign in' ELSE 'no password' END AS login_state
FROM dbo.[User]
ORDER BY user_id;
