/* ---------------------------------------------------------------------------
   Altrium CRM - let activity be logged against a lead, not only a deal.

   Until now dbo.Engagement could only point at a deal, and deal_id was NOT NULL.
   That forced the wrong order of work: a rep had to convert a lead into a deal
   before they could record the call that justified converting it.

   After this an engagement belongs to exactly one of a lead or a deal.

   Additive: existing rows keep their deal_id, and code that always sends a deal
   carries on working, so this can be run before or after the API is deployed.
   Single batch (no GO) for the Azure portal Query editor.
   --------------------------------------------------------------------------- */

SET NOCOUNT ON;

/* 1. Where the activity happened, when it is a lead rather than a deal. */
IF COL_LENGTH('dbo.Engagement', 'lead_id') IS NULL
BEGIN
    ALTER TABLE dbo.Engagement ADD lead_id INT NULL;
    PRINT 'lead_id added to dbo.Engagement.';
END
ELSE
    PRINT 'lead_id already present.';

/* 2. A deal is no longer compulsory. The foreign key is unaffected: it still
      rejects a deal_id that does not exist, it just tolerates NULL now. */
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.Engagement') AND name = 'deal_id' AND is_nullable = 0)
BEGIN
    ALTER TABLE dbo.Engagement ALTER COLUMN deal_id INT NULL;
    PRINT 'deal_id is now nullable.';
END
ELSE
    PRINT 'deal_id is already nullable.';
