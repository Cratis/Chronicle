// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public abstract partial class UniqueConstraintsStorageV2SqlContract
{
    protected virtual bool OwnerSpacesEqual => false;

    [Fact]
    public async Task should_round_trip_an_owner_at_the_450_utf16_unit_boundary()
    {
        await Initialize();
        EventSourceId owner = new string('界', 450);
        (await _storage.Save(owner, _definition, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        _storage = await ReopenStorage();
        (await _storage.GetFor(owner, _definition, _scope)).ShouldContainOnly(_value);
        (await _storage.IsAllowed(owner, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_refuse_overlength_owners_on_every_owner_api_before_creating_contexts()
    {
        await Seed();
        EventSourceId owner = new string('界', 451);
        var contexts = _optionsCreated;
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.Save(owner, _definition, 99, _value, _scope, _authority));
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.Remove(owner, _definition, _scope, _authority));
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.RemoveValue(owner, PerValue, _value, _scope, _authority));
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.IsAllowed(owner, _definition, _value, _scope));
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.GetFor(owner, _definition, _scope));
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.GetForStream(owner, _definition, _scope));
        _optionsCreated.ShouldEqual(contexts);
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_count_utf16_units_without_truncating_surrogate_pairs()
    {
        await Initialize();
        EventSourceId boundary = string.Concat(Enumerable.Repeat("😀", 225));
        (await _storage.Save(boundary, PerValue, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        EventSourceId over = boundary.Value + "x";
        await Assert.ThrowsAsync<UniqueConstraintV2OwnerTooLong>(() => _storage.Save(over, PerValue, 11, _otherValue, _scope, _authority));
        (await _storage.GetFor(boundary, PerValue, _scope)).ShouldContainOnly(_value);
    }

    [Fact]
    public async Task should_compare_owner_case_ordinally()
    {
        await Seed();
        (await _storage.IsAllowed("OWNER", _definition, _value, _scope)).IsAllowed.ShouldBeFalse();
        (await _storage.Save("OWNER", _definition, 11, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
        (await _storage.GetFor("OWNER", _definition, _scope)).ShouldContainOnly(_otherValue);
    }

    [Fact]
    public async Task should_expose_the_documented_provider_owner_padding_semantics()
    {
        await Seed();
        (await _storage.IsAllowed("owner ", _definition, _value, _scope)).IsAllowed.ShouldEqual(OwnerSpacesEqual);
        var result = await _storage.Save("owner ", _definition, 99, _value, _scope, _authority);
        result.IsSuccess.ShouldEqual(OwnerSpacesEqual);
        if (!OwnerSpacesEqual) ShouldRefuse(result, UniqueConstraintWriteFailure.ConflictingClaim);
        await ShouldRetainClaim();
        (await _storage.Remove("owner ", _definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).Any().ShouldEqual(!OwnerSpacesEqual);
    }
}
