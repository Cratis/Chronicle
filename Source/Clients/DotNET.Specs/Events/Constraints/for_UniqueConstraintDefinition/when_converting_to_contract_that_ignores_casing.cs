// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintDefinition;

public class when_converting_to_contract_that_ignores_casing : Specification
{
    UniqueConstraintDefinition _definition;
    UniqueConstraintDefinitionContract _definitionContract;

    void Establish() => _definition = new UniqueConstraintDefinition(
        "My Constraint",
        _ => "",
        [new(new EventType("Event Type", EventTypeGeneration.First).Id, ["Name"])],
        [],
        true);

    void Because() => _definitionContract = _definition.ToContract().Definition.Value as UniqueConstraintDefinitionContract;

    [Fact] void should_ignore_casing() => _definitionContract.IgnoreCasing.ShouldBeTrue();
}
