// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Validates the serverless configuration of <see cref="ChronicleOptions"/>.
/// </summary>
internal static class ServerlessConfigurationValidation
{
    /// <summary>
    /// Validates the options. Only meaningful when serverless mode is on, except that an undefined mode is always reported.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> to validate.</param>
    /// <param name="environmentName">The name of the hosting environment.</param>
    /// <returns>Every failure found, or an empty list when the configuration is valid.</returns>
    internal static IReadOnlyList<string> Validate(ChronicleOptions options, string environmentName)
    {
        var failures = new List<string>();
        var serverless = options.Serverless;
        var isDevelopment = ServerlessEnvironment.IsDevelopment(environmentName);

        if (!Enum.IsDefined(serverless.Mode))
        {
            failures.Add($"Mode (Cratis:Chronicle:Serverless:Mode) has the undefined value {(int)serverless.Mode}.");
            return failures;
    }

    /// <summary>
    /// Formats failures as the message of <see cref="InvalidServerlessConfiguration"/>.
    /// </summary>
    /// <param name="failures">The failures to format.</param>
    /// <returns>The message.</returns>
    internal static string Format(IEnumerable<string> failures)
    {
        var builder = new StringBuilder("The serverless configuration is not valid:");
        foreach (var failure in failures)
        {
            builder.AppendLine().Append("- ").Append(failure);
        }

        return builder.ToString();
    }

    static void AddProgramIdentifierFailure(ChronicleOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.ProgramIdentifier) || options.ProgramIdentifier == ChronicleOptions.DefaultProgramIdentifier)
        {
            failures.Add("Serverless mode requires ProgramIdentifier (Cratis:Chronicle:ProgramIdentifier) to be set to a stable name for this program.");
        }
    }

    static void AddReceiverUrlFailures(Uri? receiverUrl, bool isDevelopment, List<string> failures)
    {
        if (receiverUrl is null)
        {
            return;
        }

        if (!IsHttpOrHttps(receiverUrl))
        {
            failures.Add($"ReceiverUrl (Cratis:Chronicle:Serverless:ReceiverUrl) must be an absolute http or https URL, but was {Describe(receiverUrl)}.");
        }
        else if (receiverUrl.Scheme == Uri.UriSchemeHttp && !isDevelopment)
        {
            failures.Add($"ReceiverUrl (Cratis:Chronicle:Serverless:ReceiverUrl) must use https outside Development, but was {Describe(receiverUrl)}.");
        }
    }

    static bool IsHttpOrHttps(Uri url) =>
        url.IsAbsoluteUri && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);

    static void AddDeliveryAndAuthFailures(ChronicleServerlessOptions serverless, List<string> failures)
    {
        if (serverless.Delivery.Timeout <= TimeSpan.Zero)
        {
            failures.Add($"Delivery.Timeout (Cratis:Chronicle:Serverless:Delivery:Timeout) must be greater than zero, but was '{serverless.Delivery.Timeout}'.");
        }

        var unauthenticatedNotAllowed = serverless.Auth.Type == ReceiverAuthenticationType.None && !serverless.Auth.AllowUnauthenticated;
        if (unauthenticatedNotAllowed)
        {
            failures.Add("Auth.Type None (Cratis:Chronicle:Serverless:Auth:Type) requires Cratis:Chronicle:Serverless:Auth:AllowUnauthenticated to be true.");
        }
    }

    static void AddRegistrationFailures(ChronicleServerlessOptions serverless, bool isDevelopment, List<string> failures)
    {
        if (isDevelopment)
        {
            return;
        }

        if (serverless.Registration == ServerlessRegistration.Deploy && serverless.DeploymentGeneration == 0)
        {
            failures.Add("Registration Deploy requires Cratis:Chronicle:Serverless:DeploymentGeneration outside Development. Set the generation the deployment registered the artifacts under.");
        }

        if (serverless.Registration == ServerlessRegistration.OnColdStart && !serverless.AllowOnColdStartOutsideDevelopment)
        {
            failures.Add("Registration OnColdStart outside Development requires Cratis:Chronicle:Serverless:AllowOnColdStartOutsideDevelopment to be true, or use Registration Deploy.");
        }
    }

    static void AddUndefinedEnumFailures(ChronicleServerlessOptions serverless, List<string> failures)
    {
        AddWhenUndefined(serverless.Registration, "Registration", "Registration", failures);
        AddWhenUndefined(serverless.Auth.Type, "Auth.Type", "Auth:Type", failures);
        AddWhenUndefined(serverless.DefaultDelivery, "DefaultDelivery", "DefaultDelivery", failures);
    }

    static void AddWhenUndefined<TEnum>(TEnum value, string name, string path, List<string> failures)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            failures.Add($"{name} (Cratis:Chronicle:Serverless:{path}) has the undefined value {Convert.ToInt64(value)}.");
        }
    }

    /// <summary>
    /// Describes a URL for a failure message. Relative or invalid URLs are not echoed, since their text can hold a secret such as a function key.
    /// Absolute URLs are echoed without query, fragment or user info.
    /// </summary>
    static string Describe(Uri url) =>
        url.IsAbsoluteUri ? $"'{url.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.SafeUnescaped)}'" : "a relative or invalid URL";
}
