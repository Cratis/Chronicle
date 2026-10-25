// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Describes what serverless mode gives up compared to a long-running client.
/// </summary>
internal static class ServerlessDowngrades
{
    /// <summary>
    /// Describes the downgrades.
    /// </summary>
    /// <param name="plan">The <see cref="ObserverDeliveryPlan"/> of the discovered observers, if there is one.</param>
    /// <param name="options">The <see cref="ChronicleOptions"/> holding the serverless settings.</param>
    /// <returns>One line per downgrade.</returns>
    internal static IReadOnlyList<string> Describe(ObserverDeliveryPlan? plan, ChronicleOptions options)
    {
        var lines = new List<string>();
        if (plan is { Stream.Count: > 0 })
        {
            lines.Add($"Stream reactors and reducers are skipped: [{string.Join(", ", plan.Stream)}]");
        }

        lines.Add("IReadModels watch and observable streams are unavailable");
        lines.Add("Connection lifecycle events are inert");
        lines.Add("Observer state queries are unary only");
        lines.Add("Arc live queries run in snapshot or polling mode");
        lines.Add("Hosted lifecycle occurrences are not started");

        if (options.Serverless.Auth.Type == ReceiverAuthenticationType.None)
        {
            lines.Add("The receiver does not authenticate deliveries");
        }

        return lines;
    }
}
