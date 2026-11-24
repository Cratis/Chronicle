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
using Cratis.Chronicle.Security;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_a_persisted_projection_uses_an_unsupported_sequence : given.all_dependencies
{
    readonly List<Exception> _errors = [];
    readonly List<Exception> _missingProjectionErrors = [];
    readonly List<Exception> _supportedErrors = [];
    ProjectionDefinition _projection;
    MaterializedReadModels _materializedService;
    int _bindingsBeforeSupportedReads;

    void Establish()
    {
        _projection = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("the-projection", "test-read-model");
        _readModelDefinition = _readModelDefinition with
        {
            ObserverIdentifier = "the-projection",
            Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _projectionsManager.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>()).Returns(_ => Task.FromResult<IEnumerable<ProjectionDefinition>>([_projection]));
        _materializedService = new MaterializedReadModels(_grainFactory, _readBindings, _complianceHelper, NullLogger<MaterializedReadModels>.Instance, NamespaceCasingContext.Create());
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
                await ReadEveryWay(_errors);
            }
        }

        _projectionsManager.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>()).Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));
        await ReadEveryWay(_missingProjectionErrors);
        _bindingsBeforeSupportedReads = _readBindings.ReceivedCalls().Count();

        _projectionsManager.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>()).Returns(_ => Task.FromResult<IEnumerable<ProjectionDefinition>>([_projection]));
        _projection = _projection with { Owner = ProjectionOwner.Kernel };
        await ReadPages(_supportedErrors);
        _projection = _projection with { Owner = ProjectionOwner.Client, EventSequenceId = "log" };
        await ReadPages(_supportedErrors);
        _readModelDefinition = _readModelDefinition with { ObserverType = ReadModelObserverType.Reducer };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _projectionsManager.GetProducersFor(Arg.Any<ReadModelIdentifier>(), Arg.Any<ProjectionId?>()).Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));
        await ReadPages(_supportedErrors);
    }

    async Task ReadEveryWay(List<Exception> errors)
    {
        await ReadPages(errors);
        errors.Add(await Catch.Exception(() => _service.GetInstanceByKey(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
        errors.Add(await Catch.Exception(() => _service.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
        errors.Add(await Catch.Exception(() => _service.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log", EventCount = 1 })));
        errors.Add(await Catch.Exception(() => _service.Watch(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" }).FirstAsync().ToTask()));
        errors.Add(await Catch.Exception(() => _service.DehydrateSession(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
    }

    async Task ReadPages(List<Exception> errors)
    {
        errors.Add(await Catch.Exception(() => _service.GetInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model" })));
        errors.Add(await Catch.Exception(() => _materializedService.GetInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model" })));
        errors.Add(await Catch.Exception(() => _materializedService.ObserveInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model" }).FirstAsync().ToTask()));
    }

    [Fact] void should_refuse_every_variant_and_read_path() => _errors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_check_every_non_kernel_owner() => _errors.Count.ShouldEqual(Enum.GetValues<ProjectionOwner>().Count(_ => _ != ProjectionOwner.Kernel) * 32);
    [Fact] void should_refuse_when_the_projection_definition_is_absent() => _missingProjectionErrors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_use_the_neutral_refusal_for_missing_and_unsupported_producers() => _errors.Concat(_missingProjectionErrors).Cast<RpcException>().All(_ => _.Status.Detail == SystemStoreAccess.Refused).ShouldBeTrue();
    [Fact] void should_refuse_before_resolving_a_read_binding() => _bindingsBeforeSupportedReads.ShouldEqual(0);
    [Fact] void should_allow_kernel_ordinary_and_reducer_definitions_to_reach_the_read_binding() => _supportedErrors.TrueForAll(_ => _ is BindingReached).ShouldBeTrue();
    [Fact] void should_not_read_the_sink() => _sink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_delete_a_persisted_definition() => _eventStoreStorage.Projections.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Delete").ShouldBeFalse();

    sealed class BindingReached : Exception;
}
