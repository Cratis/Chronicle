// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintConverters.when_converting_to_contract;

public class and_mode_is_per_value : Specification
{
    UniqueConstraintDefinition _definition;
    Contracts.Events.Constraints.Constraint _result;

    void Establish() => _definition = new("versions", _ => "", [new("added", ["id"])], ["removed"], false)
    {
        Mode = UniqueConstraintMode.PerValue,
        RemovalEventDefinitions = [new("removed", ["versionId"])]
    };

    void Because() => _result = _definition.ToContract();

    [Fact] void should_preserve_the_mode() => _result.Definition.Value0.Mode.ShouldEqual(Contracts.Events.Constraints.UniqueConstraintMode.PerValue);
    [Fact] void should_preserve_the_removal_properties() => _result.Definition.Value0.RemovalEventDefinitions.Single().Properties.ShouldContainOnly(["versionId"]);
    [Fact] void should_preserve_the_removal_event_type() => _result.RemovedWith.ShouldContainOnly(["removed"]);
}
