// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch;

public class with_closing_event_followed_by_covered_event : given.a_batch_with_a_closing_constraint
{
    ConstraintValidationResult _closing;
    ConstraintValidationResult _covered;

    async Task Because()
    {
        _closing = await ContextFor("Closed").Validate();
        _covered = await ContextFor("Ordinary").Validate();
    }

    [Fact] void should_accept_the_closing_event() => _closing.IsValid.ShouldBeTrue();
    [Fact] void should_refuse_the_later_covered_event() => _covered.IsValid.ShouldBeFalse();
    [Fact] void should_name_the_covered_event() => _covered.Violations.Single().EventTypeId.ShouldEqual(new EventTypeId("Ordinary"));
    [Fact] async Task should_not_write_before_the_batch_is_durable() => (await _storage.GetAll()).ShouldBeEmpty();
}
