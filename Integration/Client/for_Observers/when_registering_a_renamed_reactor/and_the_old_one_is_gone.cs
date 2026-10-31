// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_renamed_reactor;

[Collection(ChronicleCollection.Name)]
public class and_the_old_one_is_gone(ChronicleFixture fixture) : given.a_renamed_client(fixture)
{
    async Task Because() => await RegisterRenamed(true);

    [Fact] void should_register_successfully() => _failure.ShouldBeNull();
    [Fact] void should_report_the_renamed_reactor() => _renamedStore.Registration.IdentityDrift.ShouldContain(new ArtifactIdentityDrift(ArtifactKind.Reactor, typeof(Original.SendWelcomeEmail).FullName!, typeof(Renamed.SendWelcomeEmail).FullName!));
}
