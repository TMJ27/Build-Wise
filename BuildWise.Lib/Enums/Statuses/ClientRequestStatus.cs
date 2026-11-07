using System;
using System.Collections.Generic;
using System.Text;

namespace BuildWise.Lib.Enums.Statuses
{
    public enum ClientRequestStatus
    {
        Open = 1,
        Closed = 2,
        Quoted = 3,
        cancelled = 4,
        Sent_To_Client = 5,
        Pending_Verification = 6,
    }
}
