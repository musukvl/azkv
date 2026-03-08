using System.Diagnostics;

namespace AzureKvManager.Tui.Services;

public sealed class SubscriptionService
{
    public bool TrySetSubscription(string subscription, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(subscription))
        {
            return true;
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "az",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            processInfo.ArgumentList.Add("account");
            processInfo.ArgumentList.Add("set");
            processInfo.ArgumentList.Add("--subscription");
            processInfo.ArgumentList.Add(subscription);

            using var process = Process.Start(processInfo);
            if (process is null)
            {
                error = "Failed to start Azure CLI process.";
                return false;
            }

            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                return true;
            }

            error = process.StandardError.ReadToEnd().Trim();
            if (string.IsNullOrWhiteSpace(error))
            {
                error = "Failed to switch Azure subscription.";
            }

            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
