// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelUniqueConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueConstraintDefinition;
using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// What a client predating the plural form puts on the wire: one removal event on the field that now carries a
/// collection. A length-delimited field is never packed, so those are the same bytes, and it arrives here as a
/// one-element collection. The supported upgrade order brings the kernel up before its clients, so this is the
/// normal shape for the whole of that window rather than an exotic payload.
/// </summary>
/// <remarks>
/// This is the spec whose absence let a break through review. Moving the collection to a fresh field number and
/// retiring this one type-checked, generated a valid schema, and passed every spec — while making the kernel skip
/// what an older client sent and register the constraint with no removal event at all. The claimed value would
/// never be released, every later attempt to claim it would be rejected as a violation, and nothing would report
/// it: the connect-time compatibility check compares services and RPC signatures, never message field shapes.
/// </remarks>
public class and_a_single_removal_event_arrives : Specification
{
    const string ConstraintNameValue = "UniqueInvitedAddress";
    static readonly EventTypeId _sentEventTypeId = "InvitationSent";
    static readonly EventTypeId _revokedEventTypeId = "InvitationRevoked";

    Contracts.Events.Constraints.Constraint _contract;
    KernelUniqueConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = ConstraintNameValue,
        Type = Contracts.Events.Constraints.ConstraintType.Unique,
        RemovedWith = [_revokedEventTypeId.Value],
        Definition = new(new UniqueConstraintDefinitionContract
        {
            EventDefinitions = [new() { EventTypeId = _sentEventTypeId.Value, Properties = ["EmailAddress"] }]
        })
    };

    void Because() => _result = (KernelUniqueConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_keep_the_removal_event_that_was_declared() => _result.RemovedWith.ShouldContainOnly([_revokedEventTypeId]);
    [Fact] void should_still_cover_the_constrained_event() => _result.EventDefinitions.Count().ShouldEqual(1);
}
