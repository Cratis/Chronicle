// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Represents an implementation of <see cref="IBusySilos"/> that reads the target of a timed out call from Orleans' own
/// description of the message, and judges it against the local silo and the current cluster membership.
/// </summary>
/// <param name="localSilo">The <see cref="ILocalSiloDetails"/> for the silo making the calls.</param>
/// <param name="membership">The <see cref="IClusterMembershipService"/> for telling which siblings are active.</param>
/// <remarks>
/// Orleans reports a call that got no answer as a <see cref="TimeoutException"/> whose message describes the request,
/// naming the sending silo and grain followed by the target silo and grain. That target is the only record of where
/// the call went. A call made before its target was resolved has no target silo in it, and is left unjudged.
/// </remarks>
[Singleton]
public partial class BusySilos(ILocalSiloDetails localSilo, IClusterMembershipService membership) : IBusySilos
{
    [GeneratedRegex(@"->\s*\[S(?<target>\S+)\s", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TimeoutTarget { get; }

    /// <inheritdoc/>
    public BusySilo? FindBusySilo(Exception exception)
    {
        var target = FindTimeoutTarget(exception);
        if (target is null)
        {
            return null;
        }

        if (Matches(localSilo.SiloAddress, target))
        {
            return new(target, true);
        }

        var isActiveSibling = membership.CurrentSnapshot.Members.Values.Any(member =>
            member.Status == SiloStatus.Active && Matches(member.SiloAddress, target));
        return isActiveSibling ? new(target, false) : null;
    }

    static string? FindTimeoutTarget(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AggregateException aggregate)
            {
                return aggregate.InnerExceptions.Select(FindTimeoutTarget).FirstOrDefault(_ => _ is not null);
            }

            if (current is TimeoutException && TimeoutTarget.Match(current.Message) is { Success: true } match)
            {
                return match.Groups["target"].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Matches a silo against a target as Orleans wrote it, with or without the silo's generation.
    /// </summary>
    /// <remarks>
    /// With a generation, only the same incarnation matches: an earlier incarnation of the same endpoint is a silo
    /// that is gone, not a busy one.
    /// </remarks>
    /// <param name="silo">The <see cref="SiloAddress"/> to match.</param>
    /// <param name="target">The target as written in the request, without its leading silo marker.</param>
    /// <returns><see langword="true"/> when the target is the silo.</returns>
    static bool Matches(SiloAddress silo, string target) =>
        string.Equals($"S{target}", silo.ToString(), StringComparison.Ordinal) ||
        string.Equals(target, silo.Endpoint.ToString(), StringComparison.Ordinal);
}
