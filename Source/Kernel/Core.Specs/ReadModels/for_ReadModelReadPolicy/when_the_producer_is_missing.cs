// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_the_producer_is_missing : Specification
{
    readonly List<Exception> _projectionErrors = [];
    readonly List<Exception> _nonProjectionErrors = [];
    IGrainFactory _grains;
    IProjectionsManager _manager;
    ReadModelDefinition[] _definitions;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IProjectionsManager>();
        _manager.GetProjectionDefinitions().Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));
        _grains.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(_manager);
        var definition = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateReadModelDefinition("read-model") with { ObserverIdentifier = "missing-producer" };
        _definitions = Enum.GetValues<ReadModelOwner>().SelectMany(owner =>
            Enum.GetValues<ReadModelSource>().Select(source => definition with { Owner = owner, Source = source })).ToArray();
    }

    async Task Because()
    {
        foreach (var definition in _definitions)
        {
            _projectionErrors.Add(await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", definition)));
        }

        _grains.ClearReceivedCalls();
        _manager.ClearReceivedCalls();
        foreach (var definition in _definitions)
        {
            await ReadWithoutProjection(definition, ReadModelObserverType.Reducer);
            await ReadWithoutProjection(definition, ReadModelObserverType.NotSet);
        }
    }

    async Task ReadWithoutProjection(ReadModelDefinition definition, ReadModelObserverType type) =>
        _nonProjectionErrors.Add(await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", definition with { ObserverType = type })));

    [Fact] void should_refuse_projection_reads_for_every_owner_and_source() => _projectionErrors.TrueForAll(_ => _ is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_check_every_owner_and_source() => _projectionErrors.Count.ShouldEqual(Enum.GetValues<ReadModelOwner>().Length * Enum.GetValues<ReadModelSource>().Length);
    [Fact] void should_use_the_neutral_refusal() => _projectionErrors.Cast<RpcException>().All(_ => _.Status.Detail == SystemStoreAccess.Refused).ShouldBeTrue();
    [Fact] void should_allow_reducer_and_unset_reads() => _nonProjectionErrors.TrueForAll(_ => _ is null).ShouldBeTrue();
    [Fact] void should_check_both_non_projection_types_for_every_owner_and_source() => _nonProjectionErrors.Count.ShouldEqual(_definitions.Length * 2);
    [Fact] void should_not_resolve_a_manager_for_non_projection_reads() => _grains.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_query_definitions_for_non_projection_reads() => _manager.ReceivedCalls().Any().ShouldBeFalse();
}
