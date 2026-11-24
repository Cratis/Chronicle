// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache.given;

public class a_cached_database : Specification
{
    protected ManualClock _clock;
    protected ControlledProvenance _raw;
    protected SinkProvenanceCache _cache;
    protected ISinkProvenanceStorage _cached;
    protected SinkProvenanceLease _lease;

    async Task Establish()
    {
        _clock = new();
        _raw = new();
        await _raw.CompleteInventory(false);
        _cache = new(_clock);
        _cached = _cache.Bind("route", () => _raw);
        _lease = await _cached.GetLease();
        await _lease.Ensure();
        _raw.Reads = 0;
    }

    async Task Destroy() => await _cache.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}
