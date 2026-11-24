// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_removing;

public class and_a_legacy_projection_has_materialized_data : given.a_manager_with_an_orphaned_projection
{
    readonly List<Exception> _errors = [];
    ObserverRemovalResult _removal;
    IEnumerable<ProjectionDefinition> _projectionsAfterRemoval;
    ReadModelDefinition _retainedReadModel;
    IReadModelReadBindings _bindings;
    ISink _sink;

    void Establish()
    {
        _definition = _definition with { EventSequenceId = "system" };
        _state.Projections = [_definition];
        _retainedReadModel = CreateReadModelDefinition(_definition.ReadModel) with
        {
            ObserverIdentifier = _definition.Identifier.Value,
            Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _sink = Substitute.For<ISink>();
        _sink.GetInstances(Arg.Any<ReadModelContainerName>(), Arg.Any<int>(), Arg.Any<int>()).Returns(new ReadModelInstances([new ExpandoObject()], 1));
        _sink.ClearReceivedCalls();
        _bindings = Substitute.For<IReadModelReadBindings>();
        _bindings.Resolve(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>(), Arg.Any<ReadModelContainerName?>())
            .Returns(new ReadModelReadBinding(_sink, _retainedReadModel.ContainerName));
        _bindings.ClearReceivedCalls();
    }

    async Task Because()
    {
        _removal = await _grain.Remove(_definition.Identifier);
        _projectionsAfterRemoval = await _grain.GetProjectionDefinitions();
        var grains = Substitute.For<IGrainFactory>();
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(_retainedReadModel);
        grains.GetGrain<IReadModel>(Arg.Any<string>()).Returns(readModel);
        var manager = Substitute.For<IProjectionsManager>();
        manager.GetProjectionDefinitions().Returns(_ => _grain.GetProjectionDefinitions());
        grains.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(manager);
        var service = new MaterializedReadModels(grains, _bindings, Substitute.For<IReadModelsCompliance>(), NullLogger<MaterializedReadModels>.Instance, NamespaceCasingContext.Create());
        _errors.Add(await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(grains, EventStore, _retainedReadModel)));
        _errors.Add(await Catch.Exception(() => service.GetInstances(new() { EventStore = EventStore, Namespace = "namespace", ReadModel = _definition.ReadModel })));
        _errors.Add(await Catch.Exception(() => service.ObserveInstances(new() { EventStore = EventStore, Namespace = "namespace", ReadModel = _definition.ReadModel }).FirstAsync().ToTask()));
    }

    [Fact] void should_remove_the_projection() => _removal.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_remove_the_producer_from_the_manager_list() => _projectionsAfterRemoval.ShouldBeEmpty();
    [Fact] void should_refuse_policy_page_and_stream_reads_with_the_retained_read_model() => _errors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_use_the_neutral_refusal() => _errors.Cast<RpcException>().All(_ => _.Status.Detail == SystemStoreAccess.Refused).ShouldBeTrue();
    [Fact] void should_not_resolve_a_read_binding() => _bindings.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_access_the_remaining_materialization() => _sink.ReceivedCalls().Any().ShouldBeFalse();
}
