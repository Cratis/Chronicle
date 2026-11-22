// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_a_persisted_projection_uses_an_unsupported_sequence : given.all_dependencies
{
    readonly List<Exception> _errors = [];
    ProjectionDefinition _projection;
    Exception _kernelError;
    int _bindingsBeforeKernel;

    void Establish()
    {
        _projection = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("the-projection", "test-read-model");
        _readModelDefinition = _readModelDefinition with
        {
            ObserverIdentifier = "the-projection",
            Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _eventStoreStorage.Projections.Has("the-projection").Returns(true);
        _eventStoreStorage.Projections.Get("the-projection").Returns(_ => _projection);
        _readBindings.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>(), Arg.Any<ReadModelContainerName?>())
            .Returns(_ => Task.FromException<ReadModelReadBinding>(new BindingReached()));
        _readBindings.ClearReceivedCalls();
    }

    async Task Because()
    {
        foreach (var owner in Enum.GetValues<ProjectionOwner>().Where(_ => _ != ProjectionOwner.Kernel))
        {
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                _projection = _projection with { Owner = owner, EventSequenceId = sequence };
                await ReadEveryWay();
            }
        }

        _bindingsBeforeKernel = _readBindings.ReceivedCalls().Count();
        _projection = _projection with { Owner = ProjectionOwner.Kernel };
        _kernelError = await Catch.Exception(() => _service.GetInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model" }));
    }

    async Task ReadEveryWay()
    {
        _errors.Add(await Catch.Exception(() => _service.GetInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model" })));
        _errors.Add(await Catch.Exception(() => _service.GetInstanceByKey(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
        _errors.Add(await Catch.Exception(() => _service.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
        _errors.Add(await Catch.Exception(() => _service.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log", EventCount = 1 })));
        _errors.Add(await Catch.Exception(() => _service.Watch(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" }).FirstAsync().ToTask()));
        _errors.Add(await Catch.Exception(() => _service.DehydrateSession(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
    }

    [Fact] void should_refuse_every_variant_and_read_path() => _errors.All(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_check_every_non_kernel_owner() => _errors.Count.ShouldEqual(Enum.GetValues<ProjectionOwner>().Count(_ => _ != ProjectionOwner.Kernel) * 24);
    [Fact] void should_refuse_before_resolving_a_read_binding() => _bindingsBeforeKernel.ShouldEqual(0);
    [Fact] void should_allow_kernel_definitions_to_reach_the_read_binding() => _kernelError.ShouldBeOfExactType<BindingReached>();
    [Fact] void should_not_read_the_sink() => _sink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_delete_a_persisted_definition() => _eventStoreStorage.Projections.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Delete").ShouldBeFalse();

    sealed class BindingReached : Exception
    {
    }
}
