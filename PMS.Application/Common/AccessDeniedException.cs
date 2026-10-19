namespace PMS.Application.Common;

/// <summary>Thrown when the caller may not touch a record; the API maps it to HTTP 403.</summary>
public class AccessDeniedException : Exception
{
    public AccessDeniedException(string message = "Access denied.") : base(message) { }
}
