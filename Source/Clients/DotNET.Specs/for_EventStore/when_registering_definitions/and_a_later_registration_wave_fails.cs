// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_a_later_registration_wave_fails : given.an_event_store_with_buildable_definitions
{
    Exception _wireFailure;
    int _attempts;

    void Establish()
    {
        _wireFailure = new InvalidOperationException("Constraints could not be sent");
        _eventStore.Constraints.Register().Returns(_ =>
        {
            _attempts++;
            return Task.FromException(_wireFailure);
        });
    }

    async Task Because() => _failure = await Catch.Exception(() => _eventStore.RegisterDefinitions(new DeploymentGeneration(1)));

    [Fact]
    void should_propagate_the_failure() => _failure.ShouldBeSame(_wireFailure);

    [Fact]
    void should_record_the_deployment_before_the_failed_wave() => _services.EventStores.Received(1).RegisterDeployment(Arg.Any<Contracts.EventStores.RegisterDeploymentRequest>());

    [Fact]
    void should_publish_the_computed_fingerprint() => (_eventStore.Registration.Fingerprint != ArtifactFingerprint.NotSet).ShouldBeTrue();

    [Fact]
    void should_not_retry() => _attempts.ShouldEqual(1);
}
