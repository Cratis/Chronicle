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
/// <para>
/// An <see cref="AggregateException"/> is judged by the first of its inner exceptions that is a timeout with a target.
/// </para>
/// </remarks>
[Singleton]
public partial class BusySilos(ILocalSiloDetails localSilo, IClusterMembershipService membership) : IBusySilos
{
    // Format assumption (Orleans 10.4.2): CallbackData writes "Response did not arrive on time in {timeout} for message:
    // {msg}", and Message formats the request as "{Direction} [{SendingSilo} {SendingGrain}]->[{TargetSilo} {TargetGrain}]",
    // where a silo is "S{endpoint}:{generation}" and a missing target silo is empty. for_BusySilos formats a real
    // Orleans Message, so an upgrade that changes this shape fails those specs; parsing nothing falls back to the
    // bounded retries.
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
    /// Matches a silo against a target as Orleans wrote it, including the silo's generation.
    /// </summary>
    /// <remarks>
    /// Only the same incarnation matches: an earlier incarnation of the same endpoint is a silo that is gone, not a
    /// busy one, and a newer incarnation of a sibling's endpoint is not the member the call went to.
    /// </remarks>
    /// <param name="silo">The <see cref="SiloAddress"/> to match.</param>
    /// <param name="target">The target as written in the request, without its leading silo marker.</param>
    /// <returns><see langword="true"/> when the target is the silo.</returns>
    static bool Matches(SiloAddress silo, string target) =>
        string.Equals($"S{target}", silo.ToString(), StringComparison.Ordinal);
}
