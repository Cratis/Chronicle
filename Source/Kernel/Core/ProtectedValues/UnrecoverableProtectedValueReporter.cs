// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>Bounds and deduplicates unrecoverable-value diagnostics within this process.</summary>
[Singleton]
public sealed class UnrecoverableProtectedValueReporter
{
    /// <summary>The maximum number of retained diagnostic identities.</summary>
    internal const int Capacity = 4096;

    /// <summary>The absolute diagnostic deduplication window.</summary>
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    readonly object _lock = new();
    readonly Dictionary<Identity, DateTimeOffset> _reported = [];
    readonly Queue<(Identity Identity, DateTimeOffset Expires)> _expiry = new();
    readonly ILogger _logger;
    readonly TimeProvider _clock;
    readonly Action _count;

    /// <summary>Initializes the process-local reporter and its counter.</summary>
    /// <param name="logger">The diagnostic logger.</param>
    /// <param name="meter">The Chronicle meter.</param>
    public UnrecoverableProtectedValueReporter(
        ILogger<UnrecoverableProtectedValueReporter> logger,
        [FromKeyedServices(WellKnown.MeterName)] IMeter<UnrecoverableProtectedValueReporter> meter)
        : this(logger, TimeProvider.System, CreateCounter(meter))
    {
    }

    /// <summary>Initializes a reporter with an explicit clock and counter.</summary>
    /// <param name="logger">The diagnostic logger.</param>
    /// <param name="clock">The expiry clock.</param>
    /// <param name="count">Records one emitted diagnostic.</param>
    internal UnrecoverableProtectedValueReporter(ILogger logger, TimeProvider clock, Action count)
    {
        _logger = logger;
        _clock = clock;
        _count = count;
    }

    /// <summary>Finds the classified cause through metadata action wrappers.</summary>
    /// <param name="exception">The failure chain.</param>
    /// <returns>The unrecoverable cause, if present.</returns>
    internal static EncryptionKeyUnrecoverable? Find(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is EncryptionKeyUnrecoverable failure) return failure;
        }

        return null;
    }

    /// <summary>Reports the first classified failure for an identity within the window.</summary>
    /// <param name="identity">The metadata release identity.</param>
    /// <param name="failure">The classified failure.</param>
    internal void Report(Identity identity, EncryptionKeyUnrecoverable failure)
    {
        if (!Remember(identity)) return;
        _logger.UnrecoverableProtectedValue(identity.Store, identity.Namespace, identity.Identifier, identity.Path, identity.Phase, failure.InnerException?.GetType().Name ?? failure.GetType().Name, failure.AttemptedRevisions);
        _count();
    }

    static Action CreateCounter(IMeter<UnrecoverableProtectedValueReporter> meter)
    {
        var counter = meter.ActualMeter?.CreateCounter<long>(
            "chronicle.protected_values.unrecoverable", "{value}", "Unrecoverable protected values reported once per identity within the diagnostic window");
        return () => counter?.Add(1);
    }

    bool Remember(Identity identity)
    {
        lock (_lock)
        {
            var now = _clock.GetUtcNow();
            if (_reported.TryGetValue(identity, out var existing) && existing > now) return false;
            while (ShouldEvict(now))
            {
                var oldest = _expiry.Dequeue();
                _reported.Remove(oldest.Identity);
            }

            var expires = now + Lifetime;
            _reported.Add(identity, expires);
            _expiry.Enqueue((identity, expires));
            return true;
        }
    }

    bool ShouldEvict(DateTimeOffset now) =>
        _expiry.TryPeek(out var oldest) && (oldest.Expires <= now || _reported.Count >= Capacity);

    /// <summary>The bounded, process-local metadata release identity.</summary>
    /// <param name="Store">The event store.</param>
    /// <param name="Namespace">The event store namespace.</param>
    /// <param name="Identifier">The release identifier.</param>
    /// <param name="Path">The property path.</param>
    /// <param name="Phase">The metadata action.</param>
    internal sealed record Identity(EventStoreName Store, EventStoreNamespaceName Namespace, string Identifier, string Path, string Phase);
}
