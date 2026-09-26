// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_an_agreement_refusal_is_cached : given.a_decision_reader
{
    Exception _secondError;

    void Establish() => _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ => Array.Empty<ProjectionDefinition>().AsEnumerable());

    async Task Because()
    {
        await Record.ExceptionAsync(() => _reader.GetDetached<Model>("source"));
        _secondError = await Record.ExceptionAsync(() => _reader.GetDetached<Model>("source"));
    }

    [Fact] void should_refuse_the_second_read() => ((DecisionReadRefused)_secondError).Reason.ShouldEqual(DecisionReadRefusalReason.DefinitionMismatch);
    [Fact] void should_only_list_definitions_once() => _projectionService.ReceivedCalls().Count().ShouldEqual(1);
}
