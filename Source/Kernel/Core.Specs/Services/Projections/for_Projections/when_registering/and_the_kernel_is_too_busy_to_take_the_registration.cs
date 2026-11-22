// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Registrations;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_kernel_is_too_busy_to_take_the_registration : Specification
{
    Exception _exception;
    Projections _service;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
        projectionsManager.RegisterShared(Arg.Any<string>(), Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(), Arg.Any<Concepts.Projections.ProjectionOwner?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<GrainCancellationToken?>())
            .Returns(Task.FromException(new RegistrationBusyException(TimeSpan.FromSeconds(5))));

        _service = new Projections(
            grainFactory,
            Substitute.For<IExpandoObjectConverter>(),
            Substitute.For<ILanguageService>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<Chronicle.ReadModels.IReadModelsCompliance>(),
            Substitute.For<ILocalSiloDetails>(),
            AllowingConnectionOwnership.Create(),
            Microsoft.Extensions.Options.Options.Create(new Cratis.Chronicle.Configuration.ChronicleOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Projections>.Instance,
            NamespaceCasingContext.Create());
    }

    async Task Because() => _exception = await Catch.Exception(() => _service.Register(new RegisterRequest
    {
        EventStore = "event-store",
        Owner = ProjectionOwner.Server,
        Projections =
        [
            new ProjectionDefinition
            {
                EventSequenceId = "default",
                Identifier = "EmployeeListProjection",
                ReadModel = "EmployeeList",
                InitialModelState = "{}",
                All = new FromEveryDefinition()
            }
        ]
    }));

    [Fact] void should_answer_unavailable() => ((RpcException)_exception).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_say_when_to_retry() => ((RpcException)_exception).Trailers.GetValue("retry-after").ShouldEqual("5");
}
