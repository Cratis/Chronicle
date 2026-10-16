// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_multiple_projection_definitions_are_listed : given.a_decision_reader
{
    Exception _error;

    void Establish() => _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns([_definition, _definition]);

    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<Model>("source"));

    [Fact] void should_refuse_the_ambiguous_definition() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMismatch);
    [Fact] void should_explain_the_ambiguity() => _error.Message.ShouldContain("multiple projection definitions");
    [Fact] void should_not_start_the_fold() => _folds.ShouldEqual(0);
}
