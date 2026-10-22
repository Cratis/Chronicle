// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// Compares the grain contracts of a build against the release before it.
/// </summary>
/// <remarks>
/// During a rolling upgrade, silos of the baseline and silos of the current build call each other's grains. That works
/// when the current build only adds: every interface, method and grain class of the baseline is still there with the
/// same wire identity and types, and an interface that gained methods has a higher [Version] so Orleans routes
/// calls that need them to silos that have them. Anything else is a break.
/// </remarks>
public static class ContractComparison
{
    /// <summary>
    /// Finds every way the current contracts fail to serve the baseline.
    /// </summary>
    /// <param name="baseline">The contracts of the previous release.</param>
    /// <param name="current">The contracts being checked.</param>
    /// <returns>The breaks, empty when the current contracts serve the baseline.</returns>
    public static IReadOnlyList<ContractBreak> Compare(ContractManifest baseline, ContractManifest current)
    {
        var breaks = new List<ContractBreak>();

        foreach (var previous in baseline.ActiveInterfaces.OrderBy(_ => _.ClrName, StringComparer.Ordinal))
        {
            if (!current.Interfaces.TryGetValue(previous.InterfaceType, out var now) || now.Retired)
            {
                breaks.Add(new(
                    ContractBreakKind.InterfaceRemoved,
                    previous.ClrName,
                    $"Grain interface '{previous.InterfaceType}' is no longer served."));
                continue;
            }

            breaks.AddRange(CompareInterface(previous, now));
        }

        foreach (var previous in baseline.ActiveClasses.OrderBy(_ => _.ClrName, StringComparer.Ordinal))
        {
            if (!current.Classes.TryGetValue(previous.GrainType, out var now) || now.Retired)
            {
                breaks.Add(new(
                    ContractBreakKind.GrainClassRemoved,
                    previous.ClrName,
                    $"Grain type '{previous.GrainType}' is no longer served."));
            }
        }

        return breaks;
    }

    static IEnumerable<ContractBreak> CompareInterface(GrainInterfaceContract previous, GrainInterfaceContract now)
    {
        foreach (var method in previous.Methods.Values.OrderBy(_ => _.Identity, StringComparer.Ordinal))
        {
            if (!now.Methods.TryGetValue(method.Identity, out var current))
            {
                yield return new(
                    ContractBreakKind.MethodRemoved,
                    now.ClrName,
                    $"'{method.Display}' is gone. Changing a parameter type changes the wire identity, so keep the method and add a new one instead.");
            }
            else if (!string.Equals(method.Signature, current.Signature, StringComparison.Ordinal))
            {
                yield return new(
                    ContractBreakKind.MethodSignatureChanged,
                    now.ClrName,
                    $"'{method.Display}' became '{current.Display}'.");
            }
        }

        if (now.Version < previous.Version)
        {
            yield return new(
                ContractBreakKind.VersionDecreased,
                now.ClrName,
                $"[Version({previous.Version})] became [Version({now.Version})].");
        }
        else if (now.Version == previous.Version)
        {
            var added = now.Methods.Keys.Where(_ => !previous.Methods.ContainsKey(_)).Order(StringComparer.Ordinal).ToArray();
            if (added.Length > 0)
            {
                yield return new(
                    ContractBreakKind.MethodsAddedWithoutVersionIncrease,
                    now.ClrName,
                    $"Added {string.Join(", ", added.Select(_ => $"'{now.Methods[_].Display}'"))} but kept [Version({now.Version})]. Raise it to [Version({previous.Version + 1})].");
            }
        }
    }
}
