// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_removing_for_a_subject;

public class and_other_claims_are_held : given.a_unique_constraints_storage
{
    static readonly EventSourceId _erased = "erased-source";
    static readonly EventSourceId _otherPerson = "other-person-source";
    static readonly EventSourceId _impersonal = "impersonal-source";
    static readonly EventSourceId _claimant = "claimant";

    async Task Establish()
    {
        await _storage.Save(_erased, _definition, 1L, "erased-value", subject: "erased-person");
        await _storage.Save(_otherPerson, _definition, 2L, "other-value", subject: "other-person");
        await _storage.Save(_impersonal, _definition, 3L, "impersonal-value");
    }

    async Task Because() => await _storage.RemoveForSubject("erased-person");

    [Fact] async Task should_release_the_erased_subjects_value() => (await _storage.IsAllowed(_claimant, _definition, "erased-value")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_another_subjects_value() => (await _storage.IsAllowed(_claimant, _definition, "other-value")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_a_value_without_a_subject() => (await _storage.IsAllowed(_claimant, _definition, "impersonal-value")).IsAllowed.ShouldBeFalse();
}
