// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Observers.when_registering_a_renamed_reactor;

[Collection(ChronicleCollection.Name)]
public class and_the_old_one_is_still_subscribed(ChronicleFixture fixture) : given.a_renamed_client(fixture)
{
    async Task Because() => await RegisterRenamed(false);

    [Fact] void should_register_successfully() => _failure.ShouldBeNull();
    [Fact] void should_not_warn_about_another_live_service() => _renamedStore.Registration.IdentityDrift.ShouldBeEmpty();
}
