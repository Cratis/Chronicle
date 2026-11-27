// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.XUnit.Integration.for_FixtureSinkProvenanceCache;

public class when_resetting_after_database_replacement : Specification
{
    FixtureSinkProvenanceCache _cache;
    ISinkProvenanceStorage _original;
    ISinkProvenanceStorage _replacement;
    SinkProvenanceLease _oldLease;
    SinkProvenanceState _replacementState;
    object _route;
    Exception _oldHandleFailure;
    Exception _oldLeaseFailure;
    SinkProvenanceState _observed;

    async Task Establish()
    {
        _cache = new();
        _route = new();
        var storage = Substitute.For<ISinkProvenanceStorage>();
        storage.ReadState().Returns(new SinkProvenanceState(new(Guid.NewGuid()), SinkProvenanceState.CurrentInventoryVersion, false));
        _original = _cache.Bind(_route, () => storage);
        _oldLease = await _original.GetLease();
        _replacement = Substitute.For<ISinkProvenanceStorage>();
        _replacementState = new(new(Guid.NewGuid()), SinkProvenanceState.CurrentInventoryVersion, false);
        _replacement.ReadState().Returns(_replacementState);
    }

    async Task Because()
    {
        await _cache.Reset();
        _oldHandleFailure = await Catch.Exception(_original.ReadState);
        _oldLeaseFailure = await Catch.Exception(_oldLease.Ensure);
        _observed = await _cache.Bind(_route, () => _replacement).ReadState();
    }

    async Task Destroy() => await _cache.DisposeAsync();

    [Fact] void should_observe_the_replacement_database() => _observed.ShouldEqual(_replacementState);
    [Fact] void should_dispose_the_old_binding() => _oldHandleFailure.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_revoke_old_read_authority() => _oldLeaseFailure.ShouldBeOfExactType<InvalidOperationException>();
}
