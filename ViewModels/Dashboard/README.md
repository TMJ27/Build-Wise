# Overview

The main Overview page reads ClientRequest, Client, ScopeItem, ServiceCategory, Quotation and QuotationRevision through BuildWiseDbContext. OverviewModels use GUID IDs. DashboardSampleData remains only for older sample pages/workspaces and is not used by Overview.

Date ranges use Harare calendar days (UTC+02:00) against stored UTC timestamps. The 7/30/all filter applies to requests by creation date and quotations by their latest revision's creation date, as well as recent-record tabs and attention actions. Future calendar dates are excluded. All customers is a lifetime database count. Open requests excludes Closed and Cancelled.

Draft/approved counts and approved values use only the latest revision per quotation. Priced approved values are grouped by currency, without conversion or adding currencies together. Unpriced and missing totals are excluded from value summaries; valid zero values remain priced. Historical revisions do not inflate quotation totals.

The activity chart always covers the last seven days, counting revision preparation on CreationDate and approval on ApprovalDate, including historical revision events. A request contributes once to the service chart: its single category, Multiple categories, or Scope not prepared. Up to five chart buckets are shown individually and the rest grouped as Other.

Attention actions derive from request verification status, absent/unreviewed scope items and latest draft quotations. Links open database request/scope pages and the GUID quotation Upsert until the older quotation Details/approval pages are migrated. Recent quotation rows use revision snapshots directly, so requests outside the selected range do not cause lookups to fail.

Shared CSS defines legend colours. Recent-record tabs retain asynchronous loading and update the range form's selected Tab. No backend or schema changes are required beyond the existing tables.
