/* ---------------------------------------------------------------------------
   Altrium CRM - give the existing team accounts a password and reactivate them.

   Run 001_authentication.sql first. Single batch (no GO) so it pastes into the
   Azure portal Query editor.

   Accounts are addressed by user_id rather than email so a typo cannot hit the
   wrong row:

     user_id 3  Shahmi   LEADERSHIP
     user_id 2  malan    SALES MANAGER
     user_id 5  Malan    SALES REP
     user_id 1  Test Admin - deliberately left deactivated

   The plaintext passwords are NOT recorded here on purpose: this file is in a
   public repository. They were handed over separately, and every holder should
   change theirs on first sign-in with POST /api/auth/change-password.
   --------------------------------------------------------------------------- */

SET NOCOUNT ON;

/* A narrow password_hash column truncates the ~85 character hash without any
   error, and the account can then never sign in. Stop rather than corrupt it. */
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

UPDATE dbo.[User]
SET password_hash = 'PBKDF2$120000$4Sq+jcrP5VOfrkJZaNY91w==$cNM1yqhc5KcJ/SGMc8GaGR/zNRWJKqrMrAWBY92wjoQ=',
    user_role     = 'LEADERSHIP',
    is_active     = 1,
    updated_at    = SYSUTCDATETIME()
WHERE user_id = 3;

UPDATE dbo.[User]
SET password_hash = 'PBKDF2$120000$FK5TSQseBfm9St8kamzK7Q==$HEySOoifBee+CxpdMU9yEFa4QpTEW63lw1uChFy8+dw=',
    user_role     = 'SALES MANAGER',
    is_active     = 1,
    updated_at    = SYSUTCDATETIME()
WHERE user_id = 2;

UPDATE dbo.[User]
SET password_hash = 'PBKDF2$120000$uWM78vBklPGAmrl6p2rblQ==$Zx8aP9CelClfjGjZ/74nPhoEVsTRQ34mVxypAA6chxQ=',
    user_role     = 'SALES REP',
    is_active     = 1,
    updated_at    = SYSUTCDATETIME()
WHERE user_id = 5;

/* hash_len should be 85-ish for every account that can sign in. Anything much
   shorter means the column truncated it and that account is broken. */
SELECT user_id, name, email, user_role, is_active,
       CASE WHEN password_hash LIKE 'PBKDF2$%' THEN 'can sign in' ELSE 'no password' END AS login_state,
       LEN(password_hash) AS hash_len
FROM dbo.[User]
ORDER BY user_id;
