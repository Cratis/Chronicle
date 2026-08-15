// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelUniqueConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueConstraintDefinition;
using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// The control for the two specs either side of it. A constraint that declares no removal event at all is the
/// common case, and the conversion must not invent one — otherwise a conversion that answered something for every
/// payload would read as a pass.
/// </summary>
public class and_no_removal_event_arrives : Specification
{
    static readonly EventTypeId _sentEventTypeId = "InvitationSent";

    Contracts.Events.Constraints.Constraint _contract;
    KernelUniqueConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = "UniqueInvitedAddress",
        Type = Contracts.Events.Constraints.ConstraintType.Unique,
        Definition = new(new UniqueConstraintDefinitionContract
        {
            EventDefinitions = [new() { EventTypeId = _sentEventTypeId.Value, Properties = ["EmailAddress"] }]
        })
    };

    void Because() => _result = (KernelUniqueConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_release_on_nothing() => _result.RemovedWith.ShouldBeEmpty();
}
