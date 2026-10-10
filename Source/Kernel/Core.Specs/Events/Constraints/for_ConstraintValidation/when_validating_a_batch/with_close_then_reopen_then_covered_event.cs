// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch;

public class with_close_then_reopen_then_covered_event : given.a_batch_with_a_closing_constraint
{
    ConstraintValidationResult[] _results;

    async Task Because() => _results =
    [
        await ContextFor("Closed").Validate(),
        await ContextFor("Reopened").Validate(),
        await ContextFor("Ordinary").Validate()
    ];

    [Fact] void should_accept_every_event_in_order() => _results.All(result => result.IsValid).ShouldBeTrue();
}
