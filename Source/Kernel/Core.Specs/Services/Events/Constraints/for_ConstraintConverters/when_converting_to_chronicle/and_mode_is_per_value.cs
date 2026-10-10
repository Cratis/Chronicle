// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

public class and_mode_is_per_value : Specification
{
    Contracts.Events.Constraints.Constraint _contract;
    UniqueConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = "version",
        Type = Contracts.Events.Constraints.ConstraintType.Unique,
        RemovedWith = ["removed"],
        Definition = new(new Contracts.Events.Constraints.UniqueConstraintDefinition
        {
            Mode = Contracts.Events.Constraints.UniqueConstraintMode.PerValue,
            EventDefinitions = [new() { EventTypeId = "added", Properties = ["id"] }],
            RemovalEventDefinitions = [new() { EventTypeId = "removed", Properties = ["versionId"] }]
        })
    };

    void Because() => _result = (UniqueConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_preserve_the_mode() => _result.Mode.ShouldEqual(UniqueConstraintMode.PerValue);
    [Fact] void should_preserve_the_removal_properties() => _result.RemovalEventDefinitions.Single().Properties.ShouldContainOnly(["versionId"]);
}
