// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using ContractProjections = Cratis.Chronicle.Contracts.Projections.IProjections;

namespace Cratis.Chronicle.Projections.for_Projections.when_registering;

public class and_the_kernel_cannot_apply_the_registration : given.all_dependencies
{
    Projections _projections;
    ContractProjections _contractProjections;
    Exception _exception;

    async Task Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"test-event-store");

        _contractProjections = Substitute.For<ContractProjections>();
        var services = Substitute.For<IServices>();
        services.Projections.Returns(_contractProjections);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _eventStore.Connection.Returns(connection);
        _contractProjections.GetRegistrationStatus(Arg.Any<GetRegistrationStatusRequest>(), Arg.Any<CallContext>())
            .Returns(Task.FromResult(new RegistrationStatus { State = RegistrationState.Failed, Failure = "the read model schema conflicts" }));

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance);

        await _projections.Discover();
    }

    async Task Because() => _exception = await Catch.Exception(_projections.Register);

    [Fact] void should_fail_the_registration() => _exception.ShouldBeOfExactType<ProjectionRegistrationNotApplied>();
    [Fact] void should_say_why() => _exception.Message.ShouldContain("the read model schema conflicts");
}
