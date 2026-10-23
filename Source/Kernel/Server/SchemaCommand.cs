// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Parses only Chronicle's one-shot schema flags before configuration sees them.
/// </summary>
/// <param name="Action">The explicit action, or None for configuration/default behavior.</param>
/// <param name="Scope">Additional stores and namespaces.</param>
/// <param name="RemainingArgs">Unconsumed host configuration arguments.</param>
internal record SchemaCommand(SchemaAction Action, SchemaScope Scope, string[] RemainingArgs)
{
    /// <summary>
    /// Consumes one-shot schema flags and preserves host arguments.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The parsed schema command.</returns>
    /// <exception cref="ArgumentException">The schema flags are invalid or combined incorrectly.</exception>
    internal static SchemaCommand Parse(string[] args)
    {
        var action = SchemaAction.None;
        var stores = new List<string>();
        var namespaces = new List<string>();
        var remaining = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--migrate-only":
                case "--verify-schema":
                    var requested = args[index] == "--migrate-only" ? SchemaAction.MigrateAndExit : SchemaAction.VerifyAndExit;
                    if (action != SchemaAction.None && action != requested)
                    {
                        throw new ArgumentException("--migrate-only and --verify-schema cannot be used together.");
                    }

                    action = requested;
                    break;
                case "--event-store":
                case "--namespace":
                    var flag = args[index];
                    if (index + 1 == args.Length || string.IsNullOrWhiteSpace(args[index + 1]) || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"{flag} requires a value.");
                    }

                    (flag == "--event-store" ? stores : namespaces).Add(args[++index]);
                    break;
                default:
                    remaining.Add(args[index]);
                    break;
            }
        }

        if (namespaces.Count != 0 && stores.Count == 0)
        {
            throw new ArgumentException("--namespace requires at least one --event-store.");
        }

        return new(action, new(stores.Distinct(StringComparer.Ordinal).ToArray(), namespaces.Distinct(StringComparer.Ordinal).ToArray()), remaining.ToArray());
    }
}
