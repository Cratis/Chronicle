// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_allow_an_unclaimed_value_without_claiming_it()
    {
        var result = await _storage.IsAllowed(_owner, _definition, _value, _scope);
        result.IsAllowed.ShouldBeTrue();
        result.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
        ShouldRefuse(await Mutation(0, _authority), UniqueConstraintWriteFailure.MissingAuthority);
    }

    [Fact]
    public async Task should_report_the_original_sequence_to_both_owners()
    {
        await Seed();
        var same = await _storage.IsAllowed(_owner, _definition, _value, _scope);
        same.IsAllowed.ShouldBeTrue();
        same.SequenceNumber.ShouldEqual((EventSequenceNumber)10);
        var other = await _storage.IsAllowed(_otherOwner, _definition, _value, _scope);
        other.IsAllowed.ShouldBeFalse();
        other.SequenceNumber.ShouldEqual((EventSequenceNumber)10);
    }

    [Fact]
    public async Task should_refuse_a_conflicting_replacement_without_releasing_the_old_value()
    {
        await Seed();
        (await _storage.Save(_otherOwner, _definition, 11, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        ShouldRefuse(await _storage.Save(_owner, _definition, 12, _otherValue, _scope, _authority), UniqueConstraintWriteFailure.ConflictingClaim);
        await ShouldRetainClaim();
        (await _storage.GetFor(_otherOwner, _definition, _scope)).ShouldContainOnly(_otherValue);
    }

    [Fact]
    public async Task should_replace_only_the_owners_value_in_per_event_source_mode()
    {
        await Seed();
        (await _storage.Save(_owner, _definition, 11, _value, _otherScope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_owner, _definition, 12, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldContainOnly(_otherValue);
        (await _storage.GetFor(_owner, _definition, _otherScope)).ShouldContainOnly(_value);
        (await _storage.IsAllowed(_otherOwner, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_retain_multiple_values_in_per_value_mode()
    {
        await Seed(PerValue);
        (await _storage.Save(_owner, PerValue, 11, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        var values = (await _storage.GetFor(_owner, PerValue, _scope)).ToArray();
        values.Length.ShouldEqual(2);
        values.ShouldContain(_value);
        values.ShouldContain(_otherValue);
    }

    [Fact]
    public async Task should_isolate_claims_by_mode_while_sharing_authority()
    {
        await Seed();
        (await _storage.Save(_otherOwner, PerValue, 11, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.IsAllowed(_otherOwner, PerValue, _value, _scope)).IsAllowed.ShouldBeTrue();
        (await _storage.IsAllowed(_otherOwner, _definition, _value, _scope)).IsAllowed.ShouldBeFalse();
        var next = await _storage.TryTransition(_definition.Name, _authority, Run(1));
        next.IsSuccess.ShouldBeTrue();
        ShouldRefuse(await _storage.Save(_owner, PerValue, 12, _otherValue, _scope, _authority), UniqueConstraintWriteFailure.AuthorityMismatch);
    }

    [Fact]
    public async Task should_isolate_authorities_and_claims_by_constraint()
    {
        await Seed();
        var other = _definition with { Name = "other-constraint" };
        (await _storage.IsAllowed(_otherOwner, other, _value, _scope)).IsAllowed.ShouldBeTrue();
        ShouldRefuse(await _storage.Save(_otherOwner, other, 11, _value, _scope, _authority), UniqueConstraintWriteFailure.MissingAuthority);
        (await _storage.InitializeAuthority(other.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_otherOwner, other, 11, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveAllFor(other, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_refresh_subject_without_replacing_the_first_sequence()
    {
        await Seed();
        (await _storage.Save(_owner, _definition, 99, _value, _scope, _authority, "replacement-subject")).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
        (await _storage.RemoveForSubject(_definition, "replacement-subject", _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
    }

    [Fact]
    public async Task should_clear_subject_metadata_on_a_repeat_with_null_subject()
    {
        await Seed();
        (await _storage.Save(_owner, _definition, 99, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_round_trip_unsigned_sequence_numbers_above_int64_max_value()
    {
        await Initialize();
        var sequence = new EventSequenceNumber(ulong.MaxValue - 1);
        (await _storage.Save(_owner, _definition, sequence, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.IsAllowed(_owner, _definition, _value, _scope)).SequenceNumber.ShouldEqual(sequence);
        (await _storage.Save(_owner, _definition, 1, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.IsAllowed(_owner, _definition, _value, _scope)).SequenceNumber.ShouldEqual(sequence);
    }
}
