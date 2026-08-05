// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_every_projection_could_be_built : given.an_event_store_with_a_projection_that_cannot_be_built
{
    RegistrationOutcome _outcome;

    void Establish() => _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
        _outcome = _eventStore.Registration;
    }

    [Fact] void should_report_registration_as_run() => _outcome.HasRun.ShouldBeTrue();
    [Fact] void should_report_the_registration_as_successful() => _outcome.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_no_failures() => _outcome.Failures.ShouldBeEmpty();
    [Fact] void should_report_the_projection_that_registered() => _outcome.Artifacts.Select(_ => _.ArtifactType).ShouldContainOnly([typeof(BuildableProjection)]);
}
