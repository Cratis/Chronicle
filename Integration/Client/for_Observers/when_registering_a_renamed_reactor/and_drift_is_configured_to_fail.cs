// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_renamed_reactor;

[Collection(ChronicleCollection.Name)]
public class and_drift_is_configured_to_fail(ChronicleFixture fixture) : given.a_renamed_client(fixture)
{
    async Task Because() => await RegisterRenamed(true, ArtifactIdentityDriftPolicy.Fail);

    [Fact] void should_refuse_registration() => _failure.ShouldBeOfExactType<ArtifactIdentityDriftDetected>();
    [Fact] void should_publish_the_failure() => _renamedStore.Registration.Failure.ShouldBeOfExactType<ArtifactIdentityDriftDetected>();
}
