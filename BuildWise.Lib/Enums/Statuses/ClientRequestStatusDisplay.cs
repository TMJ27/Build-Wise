namespace BuildWise.Lib.Enums.Statuses
{
public static class ClientRequestStatusDisplay
{
    public static string StatusName(int statusId) => (ClientRequestStatus)statusId switch
    {
        ClientRequestStatus.Open => "Open",
        ClientRequestStatus.Closed => "Closed",
        ClientRequestStatus.Quoted => "Quoted",
        ClientRequestStatus.cancelled => "Cancelled",
        ClientRequestStatus.Sent_To_Client => "Sent to client",
        ClientRequestStatus.Pending_Verification => "Pending verification",
        ClientRequestStatus.Draft => "Draft",
        _ => "Unknown"
    };

    public static string CustomerStatusName(int statusId) => (ClientRequestStatus)statusId switch
    {
        ClientRequestStatus.Open => "Submitted",
        _ => StatusName(statusId)
    };

    public static string CustomerStatusDescription(int statusId) => (ClientRequestStatus)statusId switch
    {
        ClientRequestStatus.Draft => "This request has not been sent to BuildWise. Submit it when you are ready.",
        ClientRequestStatus.Open => "BuildWise has received your request. The team will review it and update this status as work progresses.",
        ClientRequestStatus.Pending_Verification => "The BuildWise team is checking the details of your request.",
        ClientRequestStatus.Quoted => "A quotation has been prepared for this request. Check My quotations for the details.",
        ClientRequestStatus.Sent_To_Client => "A quotation has been sent to you. Open My quotations to review it.",
        ClientRequestStatus.Closed => "This request is closed.",
        ClientRequestStatus.cancelled => "This request has been cancelled.",
        _ => "Check back here for updates from the BuildWise team."
    };

    public static string IconClass(int statusId) => (ClientRequestStatus)statusId switch
    {
        ClientRequestStatus.Closed => "fa-circle-check",
        ClientRequestStatus.cancelled => "fa-ban",
        ClientRequestStatus.Draft => "fa-pen-to-square",
        ClientRequestStatus.Sent_To_Client => "fa-envelope-open-text",
        ClientRequestStatus.Quoted => "fa-file-invoice-dollar",
        ClientRequestStatus.Pending_Verification => "fa-magnifying-glass",
        _ => "fa-paper-plane"
    };
}
}
