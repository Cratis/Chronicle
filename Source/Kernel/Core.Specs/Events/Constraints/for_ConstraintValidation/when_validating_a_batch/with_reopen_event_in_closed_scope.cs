// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch;

public class with_reopen_event_in_closed_scope : given.a_batch_with_a_closing_constraint
{
    ConstraintValidationResult _reopened;
    ConstraintValidationResult _covered;

    async Task Establish() => await _storage.Close(new(_scope, "closing", EventSequenceNumber.First, null));

    async Task Because()
    {
        _reopened = await ContextFor("Reopened").Validate();
        _covered = await ContextFor("Ordinary").Validate();
    }

    [Fact] void should_exempt_the_reopen_event_from_its_owner() => _reopened.IsValid.ShouldBeTrue();
    [Fact] void should_release_the_persisted_closure_for_the_later_event() => _covered.IsValid.ShouldBeTrue();
}
