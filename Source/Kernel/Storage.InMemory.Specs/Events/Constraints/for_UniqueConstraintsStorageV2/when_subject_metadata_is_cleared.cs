// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_subject_metadata_is_cleared : given.a_seeded_v2_store
{
    async Task Because()
    {
        (await _storage.Save(_source, _definition, 20, _value, _scope, _authority, null)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
    }

    [Fact] async Task should_not_remove_the_claim_by_its_old_subject() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
    [Fact] async Task should_keep_the_original_sequence() => (await _storage.IsAllowed(_source, _definition, _value, _scope)).SequenceNumber.Value.ShouldEqual(10UL);
}
