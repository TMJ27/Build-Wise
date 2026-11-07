using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Services.Requests;

public static class RequestDisplay
{
    public static string StatusName(int statusId) => (ClientRequestStatus)statusId switch
    {
        ClientRequestStatus.Open => "Open",
        ClientRequestStatus.Closed => "Closed",
        ClientRequestStatus.Quoted => "Quoted",
        ClientRequestStatus.cancelled => "Cancelled",
        ClientRequestStatus.Sent_To_Client => "Sent to client",
        ClientRequestStatus.Pending_Verification => "Pending verification",
        _ => "Unknown"
    };
}
