# Request scope screens

## Dependencies

Open View dependencies on Scope Item Details. `Dependencies/Index/{scopeItemId}` lists prerequisites; Details and Upsert use a dependency GUID and retain its parent scope item context. Data is stored in the existing ScopeDependency table; no migration is needed. Each link means the current item depends on another work item from the same request.

The form offers same-request items excluding itself. Saving validates the target exists, rejects self/cross-request/duplicate links and traverses the request's graph to reject direct and indirect cycles, excluding the existing link on edit. Relational saves use a serializable transaction; on SQL Server an update lock on the request row serializes dependency edits before graph validation. All dependency writes must use this path for that concurrency protection.

Creator and UTC creation date are recorded on creation and preserved on edit. Notes are stored as JSON with a notes string, while legacy unchanged content is preserved. Changes reset the source item's scope review; unchanged saves preserve it. Closed/cancelled requests are read-only. Screens use GUID routes, shared CSS and breadcrumbs without Back/Cancel buttons.

## Work stages

Open View stages on Scope Item Details to access `Pages/Stages/Index/{scopeItemId}`. Rows open `Stages/Details/{id}`; Add stage and Edit stage open `Stages/Upsert/{id?}?scopeItemId=...`. All IDs are GUIDs. The pages use the existing ScopeStage table and shared CSS, with breadcrumb navigation and no Back/Cancel buttons.

Stage names are required and limited to the existing 50-character column; optional descriptions are limited to 4000 characters. New stages record the authenticated creator and UTC creation date. Edits preserve these creation fields and cannot change parent item. Changes reset scope review to Pending; unchanged saves preserve review. Closed/cancelled requests are read-only. Stages are listed by CreationDate, then GUID; the existing table has no sequence/reordering field. No migration is required.

Scope is a presentation of `ClientRequest` and its `ScopeItems`; it is not a separate database entity. No Scope table or migration is needed.

- `Pages/Index`: request list with scope item and assessment counts.
- `Pages/Details/{id}`: request overview and its scope items; `id` is the `ClientRequest.Id` GUID.
- `Pages/Items/Details/{id}`: individual item details; `id` is the `ScopeItem.Id` GUID.
- `Pages/Items/Upsert/{id?}?requestId=...`: add/edit a scope item for a request. Both IDs are GUIDs.
- `Pages/Clauses/Details/{id}` and `Pages/Clauses/Upsert/{id?}?scopeItemId=...`: view and add/edit clauses for a scope item, using GUIDs.
- `Pages/Clauses/Index/{scopeItemId}`: dedicated clause register for a single scope item. Open it with View clauses on Scope Item Details; rows link to Details and the action bar links to Upsert. It remains viewable for closed/cancelled requests.

The pages read and write the existing database tables using BuildWiseDbContext. All requests are listed, including requests without items. Closed and cancelled requests remain visible but cannot be edited. All styling uses existing classes from `wwwroot/css/site.css`; navigation uses breadcrumbs without Back or Cancel buttons. Configuration Inclusions is unchanged.

## Behaviour

Index and Details load request/customer information and scope items. Item Details loads service and reviewer information and verifies request context. Upsert loads active request/service options, validates posted IDs and field limits, and returns 404 for missing records. Existing request and service associations are fixed when editing, including existing inactive services. Edits preserve related stages, clauses, dependencies and quotation snapshots.

Save writes directly to ScopeItem with GUID keys. New items snapshot the service display code from the existing generated-number helper and use the service's active default unit if no unit is entered. Service-required site assessments remain required. Assessment notes are stored as JSON with a `notes` string; legacy text and JSON strings can also be read, and unchanged legacy content is preserved. SourceId is left unset for manual creation and preserved on edit.

ScopeReviewStatus uses Pending = 0 and Reviewed = 1. The review checkbox records the authenticated user and UTC review time; unchecking it clears ReviewedOn and marks the item pending. The existing schema requires ReviewedByUserId even while pending, so that field contains the user saving the pending item and is only displayed as reviewer when ReviewedOn exists. No schema migration is required.

Scope clauses are saved in the existing ScopeClause table and listed on Scope Item Details. ScopeClauseType uses Inclusion = 1, Exclusion = 2 and Condition = 3. Creating a clause records its authenticated creator and UTC creation date; editing preserves those creation fields and cannot move the clause to another scope item. Closed/cancelled requests are read-only. Adding or changing clause wording/type resets the item to Pending review and clears ReviewedOn; saving unchanged wording preserves the review. Unknown legacy clause type values display as Unknown and require choosing a supported type when edited. No migration or separate clause configuration table is introduced.

The request overview links to quotation creation with the request GUID once scope items exist. Saved items are available through the quotation form's Load request scope action. Requests Index, Create and Details now use database ClientRequests and GUID routes; a newly created request appears in Scope immediately, even without items. Other older sample pages still need their integer request links migrated to database GUIDs separately.
