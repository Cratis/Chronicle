// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_removing_for_a_subject;

public class and_subjects_differ_in_spelling : given.a_unique_constraints_storage
{
    static readonly EventSourceId _claimant = "claimant";

    async Task Establish()
    {
        await _storage.Save("lower-source", _definition, 1L, "lower-value", subject: "alice");
        await _storage.Save("upper-source", _definition, 2L, "upper-value", subject: "Alice");
    }

    async Task Because() => await _storage.RemoveForSubject("alice");

    [Fact] async Task should_release_the_value_of_the_subject() => (await _storage.IsAllowed(_claimant, _definition, "lower-value")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_the_value_of_the_other_spelling() => (await _storage.IsAllowed(_claimant, _definition, "upper-value")).IsAllowed.ShouldBeFalse();
}
