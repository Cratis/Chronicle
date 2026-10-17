// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_definitions_arrive_after_the_first_check : given.a_decision_reader
{
    DecisionRead<Model> _read;
    int _listingCalls;

    void Establish() => _projectionService.GetAllDefinitions(Arg.Any<GetAllDefinitionsRequest>(), Arg.Any<CallContext>())
        .Returns(_ => ++_listingCalls == 1 ? Array.Empty<ProjectionDefinition>().AsEnumerable() : [_definition]);

    async Task Because() => _read = await _reader.GetDetached<Model>("source");

    [Fact] void should_return_the_read() => _read.Exists.ShouldBeTrue();
    [Fact] void should_recheck_once() => _listingCalls.ShouldEqual(2);
    [Fact] void should_only_fold_once() => _folds.ShouldEqual(1);
}
