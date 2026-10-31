// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_removing_for_a_subject;

[Collection(MongoDBCollection.Name)]
public class and_other_claims_are_held(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    UniqueConstraintDefinition _perValue;

    async Task Establish()
    {
        _perValue = new("versions", []) { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save("erased-source", _definition, 1, "erased-value", subject: "erased-person");
        await _storage.Save("erased-source", _perValue, 2, "erased-version", subject: "erased-person");
        await _storage.Save("other-source", _definition, 3, "other-value", subject: "other-person");
        await _storage.Save("other-source", _perValue, 4, "other-version", subject: "other-person");
        await _storage.Save("impersonal-source", _perValue, 5, "impersonal-version");
    }

    async Task Because() => await _storage.RemoveForSubject("erased-person");

    [Fact] async Task should_release_the_erased_subjects_value() => (await _storage.IsAllowed("claimant", _definition, "erased-value")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_release_the_erased_subjects_retained_value() => (await _storage.IsAllowed("claimant", _perValue, "erased-version")).IsAllowed.ShouldBeTrue();
    [Fact] async Task should_keep_another_subjects_value() => (await _storage.IsAllowed("claimant", _definition, "other-value")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_another_subjects_retained_value() => (await _storage.IsAllowed("claimant", _perValue, "other-version")).IsAllowed.ShouldBeFalse();
    [Fact] async Task should_keep_a_value_without_a_subject() => (await _storage.IsAllowed("claimant", _perValue, "impersonal-version")).IsAllowed.ShouldBeFalse();
}
