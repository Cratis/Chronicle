// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_read_model_definition_is_missing : given.a_decision_reader
{
    Exception _error;

    void Establish() => _readModels.GetDefinitions(Arg.Any<GetDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(new GetDefinitionsResponse { ReadModels = [] });

    async Task Because() => _error = await Catch.Exception(() => _reader.GetDetached<Model>("source"));

    [Fact] void should_refuse_the_missing_definition() => ((DecisionReadRefused)_error).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMissing);
    [Fact] void should_name_the_missing_read_model_definition() => _error.Message.ShouldContain("read-model definition is missing on the kernel");
    [Fact] void should_recheck_once_before_refusing() => _projectionService.ReceivedCalls().Count().ShouldEqual(2);
    [Fact] void should_not_start_the_fold() => _folds.ShouldEqual(0);
}
