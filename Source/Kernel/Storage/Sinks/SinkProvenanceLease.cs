// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Provides bounded, revocable read authority for one physical database.
/// </summary>
public sealed class SinkProvenanceLease
{
    readonly SinkProvenanceCache.Entry? _entry;
    readonly bool _exempt;

    SinkProvenanceLease(bool exempt) => _exempt = exempt;

    internal SinkProvenanceLease(SinkProvenanceCache.Entry entry) => _entry = entry;

    /// <summary>
    /// Gets the explicit lease for sinks without read-model database payloads.
    /// </summary>
    public static SinkProvenanceLease Exempt { get; } = new(true);

    /// <summary>
    /// Gets a lease that authorizes no reads.
    /// </summary>
    public static SinkProvenanceLease Unavailable { get; } = new(false);

    /// <summary>
    /// Gets a token canceled when a restricted mark is observed, not merely on expiry.
    /// </summary>
    public CancellationToken Revoked => _entry?.Revoked ?? CancellationToken.None;

    /// <summary>
    /// Refreshes expired authority before a one-shot operation.
    /// </summary>
    /// <returns>The admission task.</returns>
    public async Task Ensure()
    {
        if (_exempt) return;
        if (_entry is null) throw UnavailableError();
        await _entry.Ensure();
    }

    /// <summary>
    /// Waits without emitting while clean authority is stale or temporarily unavailable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation of the waiting reader.</param>
    /// <returns>The admission task.</returns>
    public async Task WaitUntilReadable(CancellationToken cancellationToken)
    {
        if (_exempt) return;
        if (_entry is null) throw UnavailableError();
        await _entry.WaitUntilReadable(cancellationToken);
    }

    /// <summary>
    /// Checks authority and serializes delivery with publication of local denial.
    /// </summary>
    /// <param name="deliver">The final synchronous delivery.</param>
    public void Deliver(Action deliver)
    {
        if (_exempt) deliver();
        else if (_entry is not null) _entry.Deliver(deliver);
        else throw UnavailableError();
    }

    /// <summary>
    /// Rechecks authority after awaited page, compliance or serialization work.
    /// </summary>
    public void Check() => Deliver(static () => { });

    internal static InvalidOperationException UnavailableError() => new("Read-model storage is not available.");
}
