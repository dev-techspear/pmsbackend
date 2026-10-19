using Microsoft.Extensions.Logging;

namespace PMS.Application.Common;

public abstract class BaseService<T>
{
    protected readonly ILogger<T> Logger;

    protected BaseService(ILogger<T> logger)
    {
        Logger = logger;
    }

    protected void LogInfo(string message, params object[] args)
        => Logger.LogInformation("[{Service}] " + message, [typeof(T).Name, ..args]);

    protected void LogWarn(string message, params object[] args)
        => Logger.LogWarning("[{Service}] " + message, [typeof(T).Name, ..args]);

    protected void LogError(Exception ex, string message, params object[] args)
        => Logger.LogError(ex, "[{Service}] " + message, [typeof(T).Name, ..args]);

    // Executes async commands inside try-catch logging envelopes
    protected async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, string operationName)
    {
        try
        {
            LogInfo("Starting {Operation}", operationName);
            var result = await operation();
            LogInfo("Completed {Operation}", operationName);
            return result;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed {Operation}", operationName);
            throw;
        }
    }
}
