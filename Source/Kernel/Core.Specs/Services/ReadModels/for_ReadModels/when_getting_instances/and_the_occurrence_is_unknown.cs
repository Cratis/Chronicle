// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

public class and_the_occurrence_is_unknown : given.all_dependencies
{
    Exception _error;

    void Establish() =>
        _readBindings.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>(), Arg.Any<ReadModelContainerName?>())
            .Returns<Task<ReadModelReadBinding>>(_ => throw new UnknownReadModelOccurrence("test-read-model"));

    async Task Because() => _error = await Catch.Exception(() => _service.GetInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Occurrence = "x'; DROP TABLE t; --"
    }));

    [Fact] void should_fail_with_an_rpc_exception() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_report_an_invalid_argument() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.InvalidArgument);
}
