// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Describes enforced serverless behavior and APIs that are unsupported in serverless mode.
/// </summary>
internal static class ServerlessDowngrades
{
    /// <summary>
    /// Describes skipped work and unsupported APIs without enforcing restrictions on those APIs.
    /// </summary>
    /// <param name="plan">The <see cref="ObserverDeliveryPlan"/> of the discovered observers, if there is one.</param>
    /// <param name="options">The <see cref="ChronicleOptions"/> holding the serverless settings.</param>
    /// <returns>One line per skipped behavior or limitation.</returns>
    internal static IReadOnlyList<string> Describe(ObserverDeliveryPlan? plan, ChronicleOptions options)
    {
        var lines = new List<string>();
        if (plan is { Stream.Count: > 0 })
        {
            lines.Add($"Stream reactors and reducers are skipped: [{string.Join(", ", plan.Stream)}]");
        }

        lines.Add("Read model watch and observable streams are unsupported in serverless mode; the client does not prevent them from opening streams");
        lines.Add("Connection lifecycle APIs are unsupported in serverless mode; callbacks are not disabled");
        lines.Add("Observer state streams are unsupported in serverless mode; use unary queries");
        lines.Add("Arc live queries are unsupported in serverless mode; no snapshot or polling fallback is provided");
        lines.Add("Hosted lifecycle occurrences are not started");

        if (options.Serverless.Auth.Type == ReceiverAuthenticationType.None)
        {
            lines.Add("Receiver authentication is configured as None; HTTP delivery is not available in this version");
        }

        return lines;
    }
}
