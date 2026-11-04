// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_observing_instances;

public class and_the_occurrence_is_unknown : for_ReadModels.given.all_dependencies
{
    MaterializedReadModels _materializedService;
    Exception _error;

    void Establish()
    {
        _readBindings.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>(), Arg.Any<ReadModelContainerName?>())
            .Returns<Task<ReadModelReadBinding>>(_ => throw new UnknownReadModelOccurrence("test-read-model"));
        _materializedService = new(_grainFactory, _readBindings, _complianceHelper, NullLogger<MaterializedReadModels>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(async () => await _materializedService.ObserveInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Occurrence = "x'; DROP TABLE t; --"
    }).FirstAsync());

    [Fact] void should_fail_with_an_rpc_exception() => _error.ShouldBeOfExactType<RpcException>();
    [Fact] void should_report_an_invalid_argument() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.InvalidArgument);
}
