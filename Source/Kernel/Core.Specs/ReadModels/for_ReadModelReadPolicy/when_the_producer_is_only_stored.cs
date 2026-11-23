// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReadPolicy;

public class when_the_producer_is_only_stored : Specification
{
    const string Producer = "stored-producer";
    IGrainFactory _grains;
    IProjectionsManager _manager;
    ReadModelDefinition _definition;
    ProjectionDefinition _stored;
    Exception? _ordinaryError;
    Exception? _kernelError;
    Exception? _systemError;
    Exception? _kernelSystemError;
    Exception? _missingError;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IProjectionsManager>();
        _manager.GetProjectionDefinitions().Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([]));
        _grains.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(_manager);
        _grains.GetGrain<IReducersManager>(Arg.Any<string>()).Returns(Substitute.For<IReducersManager>());
        _definition = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateReadModelDefinition("read-model") with { ObserverIdentifier = Producer };
        _stored = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition(Producer, "read-model");
    }

    async Task Because()
    {
        _ordinaryError = await ReadWith(_stored with { Owner = ProjectionOwner.Client, EventSequenceId = "event-log" });
        _kernelError = await ReadWith(_stored with { Owner = ProjectionOwner.Kernel, EventSequenceId = "event-log" });
        _systemError = await ReadWith(_stored with { Owner = ProjectionOwner.Client, EventSequenceId = "system" });
        _kernelSystemError = await ReadWith(_stored with { Owner = ProjectionOwner.Kernel, EventSequenceId = "system" });
        _missingError = await ReadWith(null);
    }

    async Task<Exception?> ReadWith(ProjectionDefinition? stored)
    {
        _manager.GetStoredProjectionDefinitions().Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>(stored is null ? [] : [stored]));
        return await Catch.Exception(() => ReadModelReadPolicy.EnsureReadable(_grains, "store", _definition));
    }

    [Fact] void should_allow_a_stored_ordinary_definition() => _ordinaryError.ShouldBeNull();
    [Fact] void should_allow_a_stored_kernel_definition() => _kernelError.ShouldBeNull();
    [Fact] void should_allow_a_stored_kernel_definition_over_system() => _kernelSystemError.ShouldBeNull();
    [Fact] void should_refuse_a_stored_non_kernel_system_definition() => ((RpcException)_systemError!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_use_the_neutral_refusal_for_a_stored_system_definition() => ((RpcException)_systemError!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
    [Fact] void should_refuse_when_missing_everywhere() => ((RpcException)_missingError!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
