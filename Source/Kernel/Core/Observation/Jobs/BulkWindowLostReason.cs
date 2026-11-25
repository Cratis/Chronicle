// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Builds the deterministic reason text for a window confirmation that did not match the range-start pin.
/// </summary>
/// <remarks>
/// The confirmation only shows that a pinned window id was not returned. The text therefore says "not returned",
/// never that a window was lost or closed. Silos and ids are sorted, so the same mismatch always reads the same.
/// </remarks>
static class BulkWindowLostReason
{
    const string None = "<none>";

    /// <summary>
    /// Describes a mismatch between the range-start pin and the confirmed windows.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverDetails"/> being flushed.</param>
    /// <param name="isReplay">Whether the flush belongs to a replay.</param>
    /// <param name="handled">The handled position the flush covers.</param>
    /// <param name="durable">The durable checkpoint before the flush.</param>
    /// <param name="pin">The range-start pin, or null when no baseline was established.</param>
    /// <param name="pinAge">How long ago the pin was set, or null when unknown.</param>
    /// <param name="windows">The windows confirmed per silo.</param>
    /// <returns>The reason.</returns>
    public static string Describe(
        ObserverDetails observer,
        bool isReplay,
        EventSequenceNumber handled,
        EventSequenceNumber durable,
        IReadOnlyDictionary<SiloAddress, Guid[]>? pin,
        TimeSpan? pinAge,
        IReadOnlyDictionary<SiloAddress, Guid[]> windows)
    {
        var silos = (pin?.Keys ?? []).Union(windows.Keys).OrderBy(_ => _.ToString(), StringComparer.Ordinal).ToArray();
        var perSilo = silos.Select(silo => (Silo: silo, Expected: Ids(pin, silo), Returned: Ids(windows, silo))).ToArray();

        var perSiloText = string.Join("; ", perSilo.Select(_ => $"silo {_.Silo}: expected {Join(_.Expected)}, returned {Join(_.Returned)}"));
        var removed = perSilo.SelectMany(_ => _.Expected.Except(_.Returned)).Order().ToArray();
        var added = perSilo.SelectMany(_ => _.Returned.Except(_.Expected)).Order().ToArray();
        var missing = silos.Where(silo => pin?.ContainsKey(silo) == true && !windows.ContainsKey(silo));
        var joined = perSilo.Where(_ => pin?.ContainsKey(_.Silo) != true && _.Returned.Length > 0).Select(_ => _.Silo);

        return "windows not returned by confirmation. " +
            $"Observer {observer.Key.ObserverId} ({observer.Key.EventStore}/{observer.Key.Namespace}), mode {(isReplay ? "replay" : "catch-up")}, " +
            $"handled {handled.Value}, durable {durable.Value}, baseline {(pin is null ? "absent" : "present")}, " +
            $"pin age {(pinAge is { } age ? age.ToString() : "unknown")}. " +
            $"{perSiloText}. Changes: removed {Join(removed)}; added {Join(added)}; " +
            $"missing silos {Join(missing.Select(_ => _.ToString()))}; new silos {Join(joined.Select(_ => _.ToString()))}.";
    }

    static Guid[] Ids(IReadOnlyDictionary<SiloAddress, Guid[]>? windows, SiloAddress silo) =>
        (windows?.GetValueOrDefault(silo) ?? []).Distinct().Order().ToArray();

    static string Join<T>(IEnumerable<T> values)
    {
        var text = string.Join(", ", values);
        return text.Length == 0 ? None : text;
    }
}
