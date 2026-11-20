// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// The in-process kernel the unseen observer removal specifications run against: real storage, observers that check on their
/// client every second, and a capture of what the removal logs and counts.
/// </summary>
public abstract class UnseenObserverFixture : ChronicleInProcessFixture
{
    /// <summary>
    /// The period a client can be away before its observers are unseen, short enough to wait out.
    /// </summary>
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(5);

    readonly MeterListener _meterListener = new();
    long _removed;

    protected UnseenObserverFixture()
    {
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "chronicle.observers.unseen_removed")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>((_, measurement, _, _) => Interlocked.Add(ref _removed, measurement));
        _meterListener.Start();
    }

    /// <summary>
    /// Gets the settings of the kernel, relative to the Chronicle section.
    /// </summary>
    public abstract IReadOnlyDictionary<string, string?> Settings { get; }

    /// <summary>
    /// Gets what the kernel logged.
    /// </summary>
    public CapturedLogs Logs { get; } = new();

    /// <summary>
    /// Gets how many observers the removal has counted so far.
    /// </summary>
    public long Removed => Interlocked.Read(ref _removed);

    /// <summary>
    /// Gets the settings every variant of the kernel shares.
    /// </summary>
    protected static Dictionary<string, string?> CommonSettings => new()
    {
        ["Observers:WatchdogInterval"] = "1",
        ["Observers:UnseenRemovalAfter"] = Period.ToString("c"),
        ["Observers:UnseenRemovalBatchSize"] = "2"
    };

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        _meterListener.Dispose();
        await base.DisposeAsync();
    }
}
