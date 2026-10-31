// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_a_mismatch_is_not_cached : given.a_decision_reader
{
    Exception _firstError;
    DecisionRead<Model> _secondRead;
    int _listingCalls;

    void Establish() => _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ => ++_listingCalls <= 2
            ? [new ProjectionDefinition { Identifier = _definition.Identifier, ReadModel = "another-model" }]
            : [_definition]);

    async Task Because()
    {
        _firstError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
        _secondRead = await _reader.GetDetached<Model>("source");
    }

    [Fact] void should_refuse_the_first_read() => ((DecisionReadRefused)_firstError).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMismatch);
    [Fact] void should_name_the_read_model_difference() => _firstError.Message.ShouldContain("ReadModel");
    [Fact] void should_return_the_subsequent_read() => _secondRead.Exists.ShouldBeTrue();
    [Fact] void should_recheck_after_the_refusal() => _listingCalls.ShouldEqual(3);
}
