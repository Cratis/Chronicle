// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Selects what the in-process kernel advertises to the real client. Configure before discovery to select schema behavior.
/// </summary>
public sealed class KernelCapabilitiesForTesting
{
    /// <summary>
    /// Gets the advertised capabilities.
    /// </summary>
    public IReadOnlyCollection<string> Advertised { get; private set; } = [];

    /// <summary>
    /// Gets whether the capability check has a known result.
    /// </summary>
    public bool AreKnown { get; private set; }

    /// <summary>
    /// Advertises every current kernel capability.
    /// </summary>
    public void Current()
    {
        Advertised = [KernelCapabilities.ConcurrencyScopeConjunctions, KernelCapabilities.PreciseEventTypeSchemas, KernelCapabilities.ClosesStreamConstraints, KernelCapabilities.EffectReceipts, KernelCapabilities.Occurrences, KernelCapabilities.IdempotentAppends];
        AreKnown = true;
    }

    /// <summary>
    /// Models an older kernel that advertises no optional capabilities.
    /// </summary>
    public void Old()
    {
        Advertised = [];
        AreKnown = true;
    }

    /// <summary>
    /// Models a skipped or failed compatibility check. Capability-gated operations fail closed.
    /// </summary>
    public void Unknown()
    {
        Advertised = [];
        AreKnown = false;
    }
}
