# Customer requests

Index, Create and Details read/write ClientRequest through BuildWiseDbContext. IDs and customer/service context parameters use GUIDs. Creation selects an existing active Client, saves the authenticated creator and UTC creation date, and starts the request as Open. Field lengths match the existing database: Title, SiteAddress, ReferenceNumber and PropertyType are at most 50 characters. No migration is needed.

The reference is optional: a blank value generates a persisted date/GUID reference. Supplied references are checked against existing values case-insensitively. Request creation does not create or modify customer records. Optional serviceId context only prefills text; actual services are selected when preparing scope items.

Index supports real ClientRequestStatus values, case-insensitive search, sorting and pagination. Summary counts describe the whole database register. The old service/category filter was removed because ClientRequest has no category column; actual service selection belongs to ScopeItem.

Details links to the request's GUID in Scope, which includes requests without items. Once an editable request has scope items it offers quotation creation. Its quotation list shows each database quotation's latest revision and currently opens the database Upsert page; quotation Details/approval/Preview are still separate sample pages pending migration. Closed/cancelled requests offer scope viewing only.

Forms and details use shared site.css classes and breadcrumbs without Back/Cancel buttons. No sample workspace records are used in these Requests pages. Other older pages (such as the dashboard and Customer Details) may still contain sample integer request links; those sources must be migrated separately to database GUIDs.
