// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_registering;

public class and_the_registration_is_the_full_set : Specification
{
    Chronicle.Projections.IProjectionsManager _projectionsManager;
    Projections _service;
    string _signature;
    Concepts.Projections.Definitions.ProjectionDefinition[] _definitions;
    Concepts.Projections.ProjectionOwner? _fullSetOwner;
    string _registrant;
    string _version;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        _projectionsManager = Substitute.For<Chronicle.Projections.IProjectionsManager>();
        grainFactory.GetGrain<Chronicle.Projections.IProjectionsManager>(Arg.Any<string>()).Returns(_projectionsManager);
        _projectionsManager.RegisterShared(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(),
            Arg.Any<Concepts.Projections.ProjectionOwner?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>()).Returns(call =>
            {
                _signature = call.ArgAt<string>(0);
                _definitions = call.ArgAt<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(1).ToArray();
                _fullSetOwner = call.ArgAt<Concepts.Projections.ProjectionOwner?>(2);
                _registrant = call.ArgAt<string>(3);
                _version = call.ArgAt<string>(4);
                return Task.CompletedTask;
            });

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

    async Task Because() => await _service.Register(new RegisterRequest
    {
        EventStore = "event-store",
        Owner = ProjectionOwner.Client,
        FullSet = true,
        Registrant = "the-app",
        Version = "1.2.3",
        Projections =
        [
            new ProjectionDefinition
            {
                EventSequenceId = "default",
                Identifier = "EmployeeListProjection",
                ReadModel = "EmployeeList",
                InitialModelState = "{\"name\":\"initial\"}",
                All = new FromEveryDefinition(),
                GenerationDelivery = Contracts.Observation.EventGenerationDelivery.Pinned
            }
        ]
    });

    [Fact]
    void should_register_as_the_full_set_for_the_owner() =>
        _projectionsManager.Received(1).RegisterShared(Arg.Any<string>(),
            Arg.Any<IEnumerable<Concepts.Projections.Definitions.ProjectionDefinition>>(),
            Concepts.Projections.ProjectionOwner.Client,
            "the-app",
            "1.2.3");

    [Fact] void should_compute_the_canonical_signature_from_the_mapped_definitions_and_context() => _signature.ShouldEqual(Chronicle.Projections.ProjectionRegistrationSignature.For(_definitions, _fullSetOwner, _registrant, _version));
    [Fact] void should_map_one_definition() => _definitions.Length.ShouldEqual(1);
    [Fact] void should_map_the_projection_identifier() => _definitions.Single().Identifier.Value.ShouldEqual("EmployeeListProjection");
    [Fact] void should_map_the_read_model() => _definitions.Single().ReadModel.Value.ShouldEqual("EmployeeList");
    [Fact] void should_map_the_event_sequence() => _definitions.Single().EventSequenceId.Value.ShouldEqual("default");
    [Fact] void should_map_the_initial_model_state() => _definitions.Single().InitialModelState["name"].GetValue<string>().ShouldEqual("initial");
    [Fact] void should_map_the_definition_owner() => _definitions.Single().Owner.ShouldEqual(Concepts.Projections.ProjectionOwner.Client);
    [Fact] void should_map_the_generation_delivery() => _definitions.Single().GenerationDelivery.ShouldEqual(Concepts.Observation.EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_the_full_set_owner() => _fullSetOwner.ShouldEqual(Concepts.Projections.ProjectionOwner.Client);
    [Fact] void should_preserve_the_registrant() => _registrant.ShouldEqual("the-app");
    [Fact] void should_preserve_the_version() => _version.ShouldEqual("1.2.3");
}
