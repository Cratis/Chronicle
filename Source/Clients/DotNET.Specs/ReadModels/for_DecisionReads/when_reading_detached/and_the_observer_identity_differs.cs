// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_observer_identity_differs : given.a_decision_reader
{
    Exception _error;

    void Establish() => _readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(new GetDefinitionsResponse
        {
            ReadModels = [new ReadModelDefinition
            {
                Type = new() { Identifier = typeof(Model).GetReadModelIdentifier() },
                ObserverIdentifier = "other-projection", ObserverType = ReadModelObserverType.Projection
            }]
        });

    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<Model>("source"));

    [Fact] void should_refuse_the_definition_disagreement() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMismatch);
    [Fact] void should_name_the_identity_difference() => _error.Message.ShouldContain("ObserverIdentifier");
    [Fact] void should_not_start_the_fold() => _folds.ShouldEqual(0);
}
