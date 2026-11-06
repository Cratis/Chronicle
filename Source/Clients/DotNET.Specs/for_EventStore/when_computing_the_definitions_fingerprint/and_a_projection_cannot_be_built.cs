// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_computing_the_definitions_fingerprint;

public class and_a_projection_cannot_be_built : when_registering_all.given.an_event_store_with_a_projection_that_cannot_be_built
{
    Exception _failure;
    bool _unchanged;

    async Task Because()
    {
        var before = _eventStore.Registration;
        _failure = await Catch.Exception(_eventStore.ComputeDefinitionsFingerprint);
        _unchanged = ReferenceEquals(before, _eventStore.Registration);
    }

    [Fact] void should_report_the_incomplete_definitions() => _failure.ShouldBeOfExactType<ArtifactDefinitionsIncomplete>();
    [Fact] void should_not_publish_a_failure() => _unchanged.ShouldBeTrue();
    [Fact] void should_not_write_deployment_metadata() => _servicesAccessor.Services.EventStores.DidNotReceiveWithAnyArgs().RegisterDeployment(default!);
}
