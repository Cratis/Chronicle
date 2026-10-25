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
    /// Validates the options. Only meaningful when serverless mode is on.
    /// </summary>
    /// <param name="options">The <see cref="ChronicleOptions"/> to validate.</param>
    /// <param name="environmentName">The name of the hosting environment.</param>
    /// <returns>Every failure found, or an empty list when the configuration is valid.</returns>
    internal static IReadOnlyList<string> Validate(ChronicleOptions options, string environmentName)
    {
        var failures = new List<string>();
        var serverless = options.Serverless;
        var isDevelopment = ServerlessEnvironment.IsDevelopment(environmentName);

        if (string.IsNullOrWhiteSpace(options.ProgramIdentifier) || options.ProgramIdentifier == ChronicleOptions.DefaultProgramIdentifier)
        {
            failures.Add("Serverless mode requires ProgramIdentifier (Cratis:Chronicle:ProgramIdentifier) to be set to a stable name for this program.");
        }

        if (serverless.ReceiverUrl is not null &&
            (!serverless.ReceiverUrl.IsAbsoluteUri || (serverless.ReceiverUrl.Scheme != Uri.UriSchemeHttp && serverless.ReceiverUrl.Scheme != Uri.UriSchemeHttps)))
        {
            failures.Add($"ReceiverUrl (Cratis:Chronicle:Serverless:ReceiverUrl) must be an absolute http or https URL, but was '{serverless.ReceiverUrl}'.");
        }

        if (serverless.Auth.Type == ReceiverAuthenticationType.None && !serverless.Auth.AllowUnauthenticated)
        {
            failures.Add("Auth.Type None (Cratis:Chronicle:Serverless:Auth:Type) requires Cratis:Chronicle:Serverless:Auth:AllowUnauthenticated to be true.");
        }

        if (serverless.Registration == ServerlessRegistration.Deploy && serverless.DeploymentGeneration == 0 && !isDevelopment)
        {
            failures.Add("Registration Deploy requires Cratis:Chronicle:Serverless:DeploymentGeneration outside Development. Set the generation the deployment registered the artifacts under.");
        }

        if (serverless.Registration == ServerlessRegistration.OnColdStart && !isDevelopment && !serverless.AllowOnColdStartOutsideDevelopment)
        {
            failures.Add("Registration OnColdStart outside Development requires Cratis:Chronicle:Serverless:AllowOnColdStartOutsideDevelopment to be true, or use Registration Deploy.");
        }

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
}
