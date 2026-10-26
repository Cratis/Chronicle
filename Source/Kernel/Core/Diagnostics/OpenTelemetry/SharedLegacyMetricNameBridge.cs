// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Holds the one process-wide <see cref="LegacyMetricNameBridge"/> that every Chronicle host in the process shares.
/// </summary>
/// <remarks>
/// Meter listeners select meters by name for the whole process, and several kernels can run in one process, so each
/// host takes a lease. The bridge starts with the first lease and stops with the last, which mirrors every measurement
/// exactly once however many hosts there are, and one host stopping does not stop the mirroring for the others.
/// </remarks>
internal static class SharedLegacyMetricNameBridge
{
    static readonly Lock _lock = new();
    static LegacyMetricNameBridge? _bridge;
    static int _leases;

    /// <summary>
    /// Acquires a lease on the shared bridge, starting it when this is the first lease.
    /// </summary>
    /// <returns>The lease, which releases the bridge when disposed.</returns>
    internal static IDisposable Acquire()
    {
        lock (_lock)
        {
            _bridge ??= new LegacyMetricNameBridge(
                meter => LegacyTelemetryNames.Meters.Contains(meter.Name, StringComparer.Ordinal),
                LegacyTelemetryNames.Metrics);
            _leases++;
        }

        return new Lease();
    }

    static void Release()
    {
        lock (_lock)
        {
            if (--_leases > 0)
            {
                return;
            }

            _bridge?.Dispose();
            _bridge = null;
        }
    }

    sealed class Lease : IDisposable
    {
        int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                Release();
            }
        }
    }
}
