namespace BuildWise.Web.Services.Email;

public class EmailSendException(string message, bool canRetry) : InvalidOperationException(message)
{
    public bool CanRetry { get; } = canRetry;
}
