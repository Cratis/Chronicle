// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidation.when_validating_a_batch;

public class with_another_owner_covering_a_reopen_event : given.a_batch_with_a_closing_constraint
{
    ConstraintValidationResult _result;

    async Task Establish() => await _storage.Close(new(_scope, "another-owner", EventSequenceNumber.First, null));

    async Task Because() => _result = await ContextFor("Reopened").Validate();

    [Fact] void should_not_exempt_other_owners() => _result.IsValid.ShouldBeFalse();
}
