// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory.when_creating;

public class and_the_constraints_apply_to_the_event_sequence : given.a_constraint_validation_factory
{
    IConstraintDefinition _scopedUnique;
    IConstraintDefinition _scopedUniqueEventType;
    IConstraintValidation _result;

    void Establish()
    {
        _scopedUnique = _unique with { EventSequences = [EventSequenceId.Log] };
        _scopedUniqueEventType = _uniqueEventType with { EventSequences = [EventSequenceId.Log] };
        _definitions.Add(_scopedUnique);
        _definitions.Add(_scopedUniqueEventType);
    }

    async Task Because() => _result = await _factory.Create(KeyFor(EventSequenceId.Log));

    [Fact] void should_create_validators_for_both() => DefinitionsValidatedBy(_result).ShouldContainOnly([_scopedUnique, _scopedUniqueEventType]);
}
