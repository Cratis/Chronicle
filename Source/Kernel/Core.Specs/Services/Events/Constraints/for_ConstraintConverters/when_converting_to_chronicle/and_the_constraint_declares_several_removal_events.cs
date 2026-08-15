// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelUniqueConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueConstraintDefinition;
using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// The removal events travel on the constraint rather than inside the definition payload, and all of them have to
/// arrive. Keeping one would leave the kernel releasing on a single terminal fact against a client that declared
/// several — the appends would keep succeeding and the value would stay claimed for every other way the lifecycle
/// can end.
/// </summary>
public class and_the_constraint_declares_several_removal_events : Specification
{
    const string ConstraintNameValue = "UniqueInvitedAddress";
    static readonly EventTypeId _sentEventTypeId = "InvitationSent";
    static readonly EventTypeId _acceptedEventTypeId = "InvitationAccepted";
    static readonly EventTypeId _revokedEventTypeId = "InvitationRevoked";

    Contracts.Events.Constraints.Constraint _contract;
    KernelUniqueConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = ConstraintNameValue,
        Type = Contracts.Events.Constraints.ConstraintType.Unique,
        RemovedWith = [_acceptedEventTypeId.Value, _revokedEventTypeId.Value],
        Definition = new(new UniqueConstraintDefinitionContract
        {
            EventDefinitions = [new() { EventTypeId = _sentEventTypeId.Value, Properties = ["EmailAddress"] }]
        })
    };

    void Because() => _result = (KernelUniqueConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_have_the_constraint_name() => _result.Name.Value.ShouldEqual(ConstraintNameValue);
    [Fact] void should_carry_every_removal_event() => _result.RemovedWith.ShouldContainOnly([_acceptedEventTypeId, _revokedEventTypeId]);
}
