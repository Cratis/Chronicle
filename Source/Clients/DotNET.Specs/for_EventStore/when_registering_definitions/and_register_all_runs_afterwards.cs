// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.for_EventStore.when_registering_definitions;

public class and_register_all_runs_afterwards : given.an_event_store_with_buildable_definitions
{
    async Task Because()
    {
        _result = await _eventStore.RegisterDefinitions(new DeploymentGeneration(1));
        await _eventStore.RegisterAll();
    }

    [Fact]
    void should_clear_the_outcome_fingerprint() => _eventStore.Registration.Fingerprint.ShouldEqual(ArtifactFingerprint.NotSet);

    [Fact]
    void should_preserve_the_call_result() => (_result.Fingerprint != ArtifactFingerprint.NotSet).ShouldBeTrue();
}
