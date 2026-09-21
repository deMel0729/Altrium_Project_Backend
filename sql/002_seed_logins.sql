/* ---------------------------------------------------------------------------
   Altrium CRM - create the first sign-in accounts.

   Run 001_authentication.sql FIRST, then paste this whole file into:
     Azure portal > SQL database 'altrium-crm-db' > Query editor
   (the portal editor is allowed through the firewall, so it works from anywhere)

   The password_hash values below are real PBKDF2 hashes produced by the API's
   own PasswordHasher and verified against these passwords:

     lead@altrium.test      LEADERSHIP      Kestrel-Marlin-7412
     manager@altrium.test   SALES MANAGER   Juniper-Falcon-3806
     rep@altrium.test       SALES REP       Amber-Otter-5193

   These are demo credentials in a public repository. Change them after the
   first sign-in with POST /api/auth/change-password, and before any real data
   goes into this system.
   --------------------------------------------------------------------------- */

SET NOCOUNT ON;

/* Guard: a narrow password_hash column silently truncates the ~85 character hash
   and nobody can ever log in. Stop here rather than create broken accounts. */
DECLARE @chars INT = (
    SELECT CASE WHEN system_type_id IN (231, 239) THEN max_length / 2 ELSE max_length END
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.[User]') AND name = 'password_hash'
);

IF @chars IS NULL
BEGIN
    RAISERROR('dbo.[User].password_hash not found - check the table name.', 16, 1);
    RETURN;
END

IF @chars < 100
BEGIN
    RAISERROR('password_hash is only %d characters wide. Run 001_authentication.sql first.', 16, 1, @chars);
    RETURN;
END

/* --- the three accounts -------------------------------------------------- */

DECLARE @accounts TABLE (
    name  NVARCHAR(100),
    email NVARCHAR(150),
    role  NVARCHAR(50),
    hash  NVARCHAR(200)
);

INSERT INTO @accounts (name, email, role, hash) VALUES
 ('Altrium Leadership', 'lead@altrium.test',    'LEADERSHIP',
  'PBKDF2$120000$GIOVs8fU+zYd/56CKGV7xw==$PiCy93H2fdg9KY+Tpifx71Y+L7Jeb5n5VdDN0iollVA='),
 ('Altrium Manager',    'manager@altrium.test', 'SALES MANAGER',
  'PBKDF2$120000$1UyDCh8oPzclmjbMuDCv3w==$IISFTWyb//EvUcp6fI/lb+v8Nntam9RkIjqhWA/6B8U='),
 ('Altrium Rep',        'rep@altrium.test',     'SALES REP',
  'PBKDF2$120000$Mn2f6SdlDu0MBHZvflMF+A==$DTnp8hcxw9lJObpyzuMPhJCeDjLjA67oRxewteLdKz0=');

/* Update the row if the email is already there, otherwise insert it. Written as
   two statements rather than MERGE, which has enough known bugs to avoid. */
UPDATE u
SET u.password_hash = a.hash,
    u.user_role     = a.role,
    u.is_active     = 1,
    u.updated_at    = SYSUTCDATETIME()
FROM dbo.[User] AS u
JOIN @accounts  AS a ON a.email = u.email;

INSERT INTO dbo.[User] (name, email, password_hash, user_role, is_active, created_at, updated_at)
SELECT a.name, a.email, a.hash, a.role, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @accounts AS a
WHERE NOT EXISTS (SELECT 1 FROM dbo.[User] AS u WHERE u.email = a.email);

/* --- check what you ended up with ---------------------------------------- */
SELECT user_id, name, email, user_role, is_active,
       CASE WHEN password_hash LIKE 'PBKDF2$%' THEN 'can sign in' ELSE 'no password' END AS login_state
FROM dbo.[User]
ORDER BY user_id;


/* ===========================================================================
   OPTIONAL 1 - give your own existing account a password instead of using the
   demo ones. Ownership of everything that account already owns is kept, and the
   password becomes Kestrel-Marlin-7412.

UPDATE dbo.[User]
SET password_hash = 'PBKDF2$120000$GIOVs8fU+zYd/56CKGV7xw==$PiCy93H2fdg9KY+Tpifx71Y+L7Jeb5n5VdDN0iollVA=',
    user_role     = 'LEADERSHIP',
    is_active     = 1,
    updated_at    = SYSUTCDATETIME()
WHERE email = 'PUT-YOUR-EXISTING-EMAIL-HERE';

   =========================================================================== */


/* ===========================================================================
   OPTIONAL 2 - make the role difference visible in the demo.

   The seeded rep owns nothing, so signing in as rep@altrium.test shows empty
   lists. That is correct behaviour, but it is a better demonstration if the rep
   owns a couple of records: sign in as the rep and see two companies, sign in as
   the manager and see all of them.

   This MOVES ownership of real rows. Read it before running it.

DECLARE @rep INT = (SELECT user_id FROM dbo.[User] WHERE email = 'rep@altrium.test');

UPDATE TOP (2) dbo.Company SET user_id = @rep WHERE is_active = 1;
UPDATE TOP (2) dbo.Leads   SET user_id = @rep WHERE is_active = 1;
UPDATE TOP (2) dbo.Deals   SET user_id = @rep WHERE is_active = 1;

   =========================================================================== */
