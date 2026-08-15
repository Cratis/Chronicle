// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using ProtoBuf;
using ConstraintContract = Cratis.Chronicle.Contracts.Events.Constraints.Constraint;
using ConstraintTypeContract = Cratis.Chronicle.Contracts.Events.Constraints.ConstraintType;
using KernelUniqueConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueConstraintDefinition;
using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// The one spec here that goes through the wire rather than around it. A client predating the plural form is
/// reproduced by its actual serialized bytes — a single removal event written to field 3 as a scalar — and those
/// bytes are read back as the current contract.
/// </summary>
/// <remarks>
/// Every other conversion spec builds the contract in memory and hands it to the converter, which pins what the
/// converter does with a field but says nothing about which field number it lives on. Retiring number 3 and moving
/// the collection to number 6 left all of them green while making the kernel drop what an older client sent, and
/// the supported upgrade order — kernel first, then clients — walks straight through that window. Only serializing
/// the older shape and deserializing the current one can tell the difference.
/// </remarks>
public class and_the_payload_came_from_a_client_predating_the_plural_form : Specification
{
    const string ConstraintNameValue = "UniqueInvitedAddress";
    static readonly EventTypeId _sentEventTypeId = "InvitationSent";
    static readonly EventTypeId _revokedEventTypeId = "InvitationRevoked";

    byte[] _payloadFromTheOlderClient;
    ConstraintContract _asReadByTheKernel;
    KernelUniqueConstraintDefinition _result;

    void Establish()
    {
        var olderClient = new ConstraintAsAnOlderClientSendsIt
        {
            Name = ConstraintNameValue,
            Type = ConstraintTypeContract.Unique,
            RemovedWith = _revokedEventTypeId.Value,
            Definition = new(new UniqueConstraintDefinitionContract
            {
                EventDefinitions = [new() { EventTypeId = _sentEventTypeId.Value, Properties = ["EmailAddress"] }]
            })
        };

        using var stream = new MemoryStream();
        Serializer.Serialize(stream, olderClient);
        _payloadFromTheOlderClient = stream.ToArray();
    }

    void Because()
    {
        using var stream = new MemoryStream(_payloadFromTheOlderClient);
        _asReadByTheKernel = Serializer.Deserialize<ConstraintContract>(stream);
        _result = (KernelUniqueConstraintDefinition)_asReadByTheKernel.ToChronicle();
    }

    [Fact] void should_read_the_removal_event_off_the_wire() => _asReadByTheKernel.RemovedWith.ShouldContainOnly([_revokedEventTypeId.Value]);
    [Fact] void should_register_the_constraint_with_that_removal_event() => _result.RemovedWith.ShouldContainOnly([_revokedEventTypeId]);
    [Fact] void should_keep_the_constraint_name() => _result.Name.Value.ShouldEqual(ConstraintNameValue);
    [Fact] void should_still_cover_the_constrained_event() => _result.EventDefinitions.Count().ShouldEqual(1);

    /// <summary>
    /// The contract as it stood while a constraint could only be released by one event: the removal event is a
    /// scalar on field 3. Everything else keeps its number, so the bytes differ from a current payload in exactly
    /// the one way under test.
    /// </summary>
    [ProtoContract]
    sealed class ConstraintAsAnOlderClientSendsIt
    {
        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(2)]
        public ConstraintTypeContract Type { get; set; }

        [ProtoMember(3)]
        public string? RemovedWith { get; set; }

        [ProtoMember(4)]
        public Contracts.Primitives.OneOf<UniqueConstraintDefinitionContract, Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition> Definition { get; set; }
    }
}
