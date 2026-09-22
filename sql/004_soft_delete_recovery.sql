/* ---------------------------------------------------------------------------
   Altrium CRM - support the "Recently deleted" recovery screen.

   Deleting a record sets is_active = 0 (a soft delete). Until now nothing
   recorded WHEN that happened, so an archive list could not be ordered or dated.
   This adds a nullable deleted_at to every soft-deletable table.

   Additive and nullable: existing code that never writes the column keeps
   working, so this can be run before or after the API is deployed.
   Single batch (no GO) so it pastes into the Azure portal Query editor.
   --------------------------------------------------------------------------- */

SET NOCOUNT ON;

IF COL_LENGTH('dbo.Company', 'deleted_at') IS NULL
    ALTER TABLE dbo.Company ADD deleted_at DATETIME2 NULL;
IF COL_LENGTH('dbo.Contact', 'deleted_at') IS NULL
    ALTER TABLE dbo.Contact ADD deleted_at DATETIME2 NULL;
IF COL_LENGTH('dbo.Leads', 'deleted_at') IS NULL
    ALTER TABLE dbo.Leads ADD deleted_at DATETIME2 NULL;
IF COL_LENGTH('dbo.Deals', 'deleted_at') IS NULL
    ALTER TABLE dbo.Deals ADD deleted_at DATETIME2 NULL;
IF COL_LENGTH('dbo.Engagement', 'deleted_at') IS NULL
    ALTER TABLE dbo.Engagement ADD deleted_at DATETIME2 NULL;
IF COL_LENGTH('dbo.follow_ups', 'deleted_at') IS NULL
    ALTER TABLE dbo.follow_ups ADD deleted_at DATETIME2 NULL;
IF COL_LENGTH('dbo.[User]', 'deleted_at') IS NULL
    ALTER TABLE dbo.[User] ADD deleted_at DATETIME2 NULL;

/* Rows archived before this column existed have no timestamp. Leave them NULL
   rather than invent one - the archive screen shows them as "date unknown". */

SELECT 'Company' AS entity, COUNT(*) AS archived_rows FROM dbo.Company WHERE is_active = 0
UNION ALL SELECT 'Contact', COUNT(*) FROM dbo.Contact WHERE is_active = 0
UNION ALL SELECT 'Leads', COUNT(*) FROM dbo.Leads WHERE is_active = 0
UNION ALL SELECT 'Deals', COUNT(*) FROM dbo.Deals WHERE is_active = 0
UNION ALL SELECT 'Engagement', COUNT(*) FROM dbo.Engagement WHERE is_active = 0
UNION ALL SELECT 'follow_ups', COUNT(*) FROM dbo.follow_ups WHERE is_active = 0
UNION ALL SELECT 'User', COUNT(*) FROM dbo.[User] WHERE is_active = 0;
