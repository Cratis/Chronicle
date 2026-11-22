// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public abstract partial class UniqueConstraintsStorageV2SqlContract
{
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
    public async Task should_refuse_same_value_claims_from_padding_distinct_owners()
    {
        await Initialize();
        foreach (var definition in new[] { _definition, PerValue })
        {
            (await _storage.Save(_owner, definition, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
            (await _storage.IsAllowed("owner ", definition, _value, _scope)).IsAllowed.ShouldBeFalse();
            ShouldRefuse(await _storage.Save("owner ", definition, 99, _value, _scope, _authority, "wrong-subject"), UniqueConstraintWriteFailure.ConflictingClaim);
            (await _storage.GetFor("owner ", definition, _scope)).ShouldBeEmpty();
            (await _storage.RemoveForSubject(definition, "wrong-subject", _authority)).IsSuccess.ShouldBeTrue();
            await ShouldRetainClaim(definition);
        }
    }

    [Fact]
    public async Task should_remove_only_exact_owner_bytes_in_both_modes()
    {
        await Initialize();
        foreach (var definition in new[] { _definition, PerValue })
        {
            (await _storage.Save(_owner, definition, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.Remove("owner ", definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.RemoveValue("owner ", definition, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            await ShouldRetainClaim(definition);
            ShouldRefuse(await _storage.Save(_otherOwner, definition, 99, _value, _scope, _authority), UniqueConstraintWriteFailure.ConflictingClaim);
        }
    }

    [Fact]
    public async Task should_refuse_an_unpadded_alias_of_an_owner_with_trailing_spaces()
    {
        await Initialize();
        foreach (var definition in new[] { _definition, PerValue })
        {
            (await _storage.Save("owner ", definition, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.IsAllowed(_owner, definition, _value, _scope)).IsAllowed.ShouldBeFalse();
            (await _storage.GetFor(_owner, definition, _scope)).ShouldBeEmpty();
            ShouldRefuse(await _storage.Save(_owner, definition, 99, _value, _scope, _authority), UniqueConstraintWriteFailure.ConflictingClaim);
            (await _storage.Remove(_owner, definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.RemoveValue(_owner, definition, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
            (await _storage.GetFor("owner ", definition, _scope)).ShouldContainOnly(_value);
        }
    }

    [Fact]
    public async Task should_preserve_an_owner_when_a_padding_distinct_replacement_is_refused_or_safe()
    {
        await Seed();
        var result = await _storage.Save("owner ", _definition, 99, _otherValue, _scope, _authority);
        await ShouldRetainClaim();
        (await _storage.IsAllowed("owner ", _definition, _value, _scope)).IsAllowed.ShouldBeFalse();
        var values = await _storage.GetFor("owner ", _definition, _scope);
        if (result.IsSuccess)
        {
            values.ShouldContainOnly(_otherValue);
        }
        else
        {
            ShouldRefuse(result, UniqueConstraintWriteFailure.ConflictingClaim);
            values.ShouldBeEmpty();
        }
    }
}
