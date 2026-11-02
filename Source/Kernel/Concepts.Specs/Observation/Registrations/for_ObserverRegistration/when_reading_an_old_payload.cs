// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Observation.Registrations.for_ObserverRegistration;

public class when_reading_an_old_payload : Specification
{
    ObserverRegistration _registration;
    ObserverOwnerSettings _settings;

    void Because()
    {
        var options = new JsonSerializerOptions { Converters = { new ConceptAsJsonConverterFactory() } };
        _registration = JsonSerializer.Deserialize<ObserverRegistration>(
            """{"ObserverId":"observer","EventSequenceId":"event-log","ObserverType":1,"Definition":{"kind":"reactor","Definition":{"Identifier":"observer","Owner":1,"EventSequenceId":"event-log","EventTypes":[]}}}""", options)!;
        _settings = JsonSerializer.Deserialize<ObserverOwnerSettings>("""{"Owner":"program","OwnerPrincipal":"subject"}""", options)!;
    }

    [Fact] void should_default_to_stream_delivery() => _registration.TargetKind.ShouldEqual(ObserverTargetKind.Stream);
    [Fact] void should_default_to_an_unknown_owner() => _registration.Owner.ShouldEqual(ProgramIdentifier.Unknown);
    [Fact] void should_default_to_an_anonymous_principal() => _registration.OwnerPrincipal.ShouldEqual(OwnerPrincipal.Anonymous);
    [Fact] void should_default_to_an_active_client_registration() => _registration.IsActive.ShouldBeTrue();
    [Fact] void should_default_to_the_stream_source() => _registration.Source.ShouldEqual(ObserverRegistrationSource.Stream);
    [Fact] void should_default_to_an_unversioned_registration() => _registration.Revision.Value.ShouldEqual(0UL);
    [Fact] void should_default_to_no_deployment_generation() => _registration.DeploymentGeneration.ShouldEqual(0UL);
    [Fact] void should_not_invent_an_http_target() => _registration.HttpTarget.ShouldBeNull();
    [Fact] void should_default_to_thirty_seconds() => _settings.HttpDeliveryTimeout.ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_keep_the_legacy_generation_policy() => ((ReactorRegistrationDefinition)_registration.Definition).Definition.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
}
