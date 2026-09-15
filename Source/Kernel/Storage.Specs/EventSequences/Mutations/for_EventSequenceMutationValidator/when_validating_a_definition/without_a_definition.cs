// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_definition;

public class without_a_definition : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _result;

    void Because() => _result = EventSequenceMutationValidator.ValidateDefinition(_scope, null);

    [Fact] void should_report_a_missing_value() => _result.Error.ShouldEqual(EventSequenceMutationValidationError.MissingValue);
}
