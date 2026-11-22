// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_removing_all_scopes : given.a_seeded_v2_store
{
    UniqueConstraintDefinition _otherConstraint;

    async Task Establish()
    {
        _otherConstraint = _definition with { Name = "other-constraint" };
        (await _storage.InitializeAuthority(_otherConstraint.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_source, _otherConstraint, 12, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_source, _definition, 11, _value, _otherScope, _authority)).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => (await _storage.RemoveAllFor(_definition, _authority)).IsSuccess.ShouldBeTrue();
    [Fact] async Task should_remove_the_first_scope() => (await _storage.GetFor(_source, _definition, _scope)).ShouldBeEmpty();
    [Fact] async Task should_remove_the_second_scope() => (await _storage.GetFor(_source, _definition, _otherScope)).ShouldBeEmpty();
    [Fact] async Task should_leave_other_constraints_unchanged() => (await _storage.GetFor(_source, _otherConstraint, _scope)).ShouldEqual([_value]);
    [Fact] async Task should_preserve_authority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
}
