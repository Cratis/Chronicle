// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_repeating_a_claim : given.a_seeded_v2_store
{
    ulong _sequence;
    UniqueConstraintValue[] _afterOldSubject;
    UniqueConstraintValue[] _afterNewSubject;

    async Task Because()
    {
        (await _storage.Save(_source, _definition, 20, _value, _scope, _authority, "new-subject")).IsSuccess.ShouldBeTrue();
        _sequence = (await _storage.IsAllowed(_source, _definition, _value, _scope)).SequenceNumber.Value;
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
        _afterOldSubject = (await _storage.GetFor(_source, _definition, _scope)).ToArray();
        (await _storage.RemoveForSubject(_definition, "new-subject", _authority)).IsSuccess.ShouldBeTrue();
        _afterNewSubject = (await _storage.GetFor(_source, _definition, _scope)).ToArray();
    }

    [Fact] void should_keep_the_original_sequence() => _sequence.ShouldEqual(10UL);
    [Fact] void should_replace_subject_metadata() => _afterOldSubject.ShouldEqual([_value]);
    [Fact] void should_remove_by_the_refreshed_subject() => _afterNewSubject.ShouldBeEmpty();
}
