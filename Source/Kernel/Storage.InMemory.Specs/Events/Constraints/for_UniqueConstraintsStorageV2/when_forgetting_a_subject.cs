// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_forgetting_a_subject : given.a_seeded_v2_store
{
    UniqueConstraintDefinition _otherConstraint;

    async Task Establish()
    {
        _otherConstraint = _definition with { Name = "other-constraint" };
        (await _storage.InitializeAuthority(_otherConstraint.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_source, _otherConstraint, 12, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_source, _definition, 11, _value, _otherScope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_otherSource, _definition, 13, _otherValue, _scope, _authority, "another-subject")).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
    [Fact] async Task should_remove_the_subject_in_the_first_scope() => (await _storage.GetFor(_source, _definition, _scope)).ShouldBeEmpty();
    [Fact] async Task should_remove_the_subject_in_the_second_scope() => (await _storage.GetFor(_source, _definition, _otherScope)).ShouldBeEmpty();
    [Fact] async Task should_keep_another_subject() => (await _storage.GetFor(_otherSource, _definition, _scope)).ShouldEqual([_otherValue]);
    [Fact] async Task should_never_cross_constraint_authority() => (await _storage.GetFor(_source, _otherConstraint, _scope)).ShouldEqual([_value]);
}
