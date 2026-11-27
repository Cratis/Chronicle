// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache.given;

public class two_routes : a_cached_database
{
    protected SinkProvenanceState _secondState;
    protected SinkProvenanceLease _secondLease;
    protected Exception? _secondFailure;

    async Task Establish()
    {
        _secondState = await _cached.ReadState();
        var second = Substitute.For<ISinkProvenanceStorage>();
        second.ReadState().Returns(_ => _secondFailure is null ? Task.FromResult(_secondState) : Task.FromException<SinkProvenanceState>(_secondFailure));
        _secondLease = await _cache.Bind("second-route", () => second).GetLease();
        await _secondLease.Ensure();
    }
}
