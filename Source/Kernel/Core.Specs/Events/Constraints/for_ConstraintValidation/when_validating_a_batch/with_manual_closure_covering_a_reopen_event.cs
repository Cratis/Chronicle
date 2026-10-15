// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch;

public class with_manual_closure_covering_a_reopen_event : given.a_batch_with_a_closing_constraint
{
    ConstraintValidationResult _result;

    async Task Establish()
    {
        await _storage.Close(new(_scope, "closing", EventSequenceNumber.First, null));
        await _storage.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
    }

    async Task Because() => _result = await ContextFor("Reopened").Validate();

    [Fact] void should_not_exempt_manual_closures() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_manual_owner() => _result.Violations.Single().Details[WellKnownConstraintDetailKeys.ClosedBy].ShouldEqual(string.Empty);
}
