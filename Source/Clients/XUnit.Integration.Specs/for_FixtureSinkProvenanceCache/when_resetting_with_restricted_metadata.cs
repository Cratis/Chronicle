// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.XUnit.Integration.for_FixtureSinkProvenanceCache;

public class when_resetting_with_restricted_metadata : Specification
{
    FixtureSinkProvenanceCache _cache;
    ISinkProvenanceStorage _storage;
    object _route;
    Exception _failure;

    async Task Establish()
    {
        _cache = new();
        _route = new();
        _storage = Substitute.For<ISinkProvenanceStorage>();
        _storage.ReadState().Returns(new SinkProvenanceState(new(Guid.NewGuid()), SinkProvenanceState.CurrentInventoryVersion, true));
        await _cache.Bind(_route, () => _storage).ReadState();
    }

    async Task Because()
    {
        await _cache.Reset();
        var lease = await _cache.Bind(_route, () => _storage).GetLease();
        _failure = await Catch.Exception(lease.Ensure);
    }

    async Task Destroy() => await _cache.DisposeAsync();

    [Fact] void should_still_refuse_reads_of_durably_restricted_data() => _failure.ShouldBeOfExactType<InvalidOperationException>();
}
