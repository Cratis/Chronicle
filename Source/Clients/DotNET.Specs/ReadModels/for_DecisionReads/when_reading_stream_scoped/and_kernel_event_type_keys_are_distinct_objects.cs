// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_stream_scoped;

public class and_kernel_event_type_keys_are_distinct_objects : given.a_decision_reader
{
    DecisionRead<Model> _result;

    void Establish()
    {
        _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
        var kernel = new ProjectionDefinition
        {
            Identifier = _definition.Identifier,
            ReadModel = _definition.ReadModel,
            EventSequenceId = _definition.EventSequenceId,
            From = new Dictionary<Contracts.Events.EventType, FromDefinition>
            {
                [new() { Id = "created", Generation = 1 }] = new() { Key = "$eventContext(eventStreamId)" }
            }
        };
        _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>()).Returns(new[] { kernel }.AsEnumerable());
        _readModels.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>(), Arg.Any<CallContext>()).Returns(new GetInstanceByKeyResponse
        {
            StreamScoped = true, LastHandledEventSequenceNumber = 4, ReadModel = "{\"id\":\"selected\"}"
        });
    }

    async Task Because() => _result = await _reader.GetDetached<Model>("source", "type", "selected");

    [Fact] void should_compare_definitions_by_event_type_identity_not_reference() => _result.IsProtected.ShouldBeTrue();
}
