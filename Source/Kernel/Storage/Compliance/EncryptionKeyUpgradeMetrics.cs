// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Metrics for encryption keys that cannot be used. The counters carry no tags, so no identifier can reach them.
/// </summary>
public static class EncryptionKeyUpgradeMetrics
{
    static readonly Meter _meter = new(WellKnown.MeterName);
    static readonly Counter<long> _refused = _meter.CreateCounter<long>("chronicle-encryption-key-upgrade-refused", description: "Number of operations refused because an encryption key is stored under an earlier name that cannot be addressed unambiguously");
    static readonly Counter<long> _unavailable = _meter.CreateCounter<long>("chronicle-encryption-key-unavailable", description: "Number of operations refused because an encryption key is disabled in the key store");

    /// <summary>
    /// Records an operation refused because the key needs the key upgrade.
    /// </summary>
    public static void Refused() => _refused.Add(1);

    /// <summary>
    /// Records an operation refused because the key is disabled.
    /// </summary>
    public static void Unavailable() => _unavailable.Add(1);
}
