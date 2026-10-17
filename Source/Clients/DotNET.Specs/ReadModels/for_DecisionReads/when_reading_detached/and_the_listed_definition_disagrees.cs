// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_the_listed_definition_disagrees : given.a_decision_reader
{
    DecisionReadRefused _error;

    void Establish() => _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ => Array.Empty<ProjectionDefinition>().AsEnumerable());

    async Task Because() => _error = await Record.ExceptionAsync(() => _reader.GetDetached<Model>("source")) as DecisionReadRefused;

    [Fact] void should_report_the_missing_definition() => _error.Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMissing);
    [Fact] void should_name_the_missing_projection() => _error.Message.ShouldContain("projection definition is missing on the kernel");
    [Fact] void should_recheck_once_before_refusing() => _projectionService.ReceivedCalls().Count().ShouldEqual(2);
    [Fact] void should_not_fold_after_mismatch() => _folds.ShouldEqual(0);
    [Fact] void should_not_query_the_tail() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
