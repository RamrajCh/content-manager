using System.Text;

namespace ContentManager.Api.Features.Admin;

public static class OwnerCommands
{
    public static async Task RunAsync(AdminAuthService auth, string[] args)
    {
        try
        {
            OwnerSetup setup;
            if (args.Contains("--provision-owner", StringComparer.Ordinal))
            {
                var index = Array.IndexOf(args, "--login");
                if (index < 0 || index + 1 >= args.Length)
                    throw new InvalidOperationException("--login <name> is required.");
                setup = await auth.ProvisionOwnerAsync(args[index + 1], ReadPassword());
            }
            else
            {
                setup = await auth.RecoverOwnerAsync(ReadPassword());
            }

            Console.WriteLine($"Owner login: {setup.Login}");
            Console.WriteLine("Add this authenticator setup now; it is shown only once:");
            Console.WriteLine($"  Secret: {setup.TotpSecret}");
            Console.WriteLine($"  URI:    {setup.OtpAuthUri}");
            Console.WriteLine("Single-use recovery codes. Store them offline:");
            foreach (var code in setup.RecoveryCodes) Console.WriteLine($"  {code}");
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Environment.ExitCode = 1;
        }
    }

    private static string ReadPassword()
    {
        if (Console.IsInputRedirected)
            return Environment.GetEnvironmentVariable("CONTENTMANAGER_OWNER_PASSWORD")
                ?? throw new InvalidOperationException(
                    "Run in an interactive terminal, or set CONTENTMANAGER_OWNER_PASSWORD for this process only.");

        var first = ReadMasked("New password: ");
        if (first != ReadMasked("Repeat password: "))
            throw new InvalidOperationException("Passwords did not match.");
        return first;
    }

    private static string ReadMasked(string prompt)
    {
        Console.Error.Write(prompt);
        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; }
            else if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
        }
        Console.Error.WriteLine();
        return value.ToString();
    }
}
