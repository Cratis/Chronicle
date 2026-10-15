// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_generation_delivery_disagrees : given.a_decision_reader
{
    Exception _error;

    void Establish() => _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ => new[]
        {
            new ProjectionDefinition
            {
                Identifier = _definition.Identifier, ReadModel = _definition.ReadModel,
                EventSequenceId = _definition.EventSequenceId, From = _definition.From,
                GenerationDelivery = EventGenerationDelivery.Pinned
            }
        }.AsEnumerable());

    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<Model>("source"));

    [Fact] void should_refuse_definition_disagreement() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMismatch);
    [Fact] void should_not_start_the_fold() => _folds.ShouldEqual(0);
}
