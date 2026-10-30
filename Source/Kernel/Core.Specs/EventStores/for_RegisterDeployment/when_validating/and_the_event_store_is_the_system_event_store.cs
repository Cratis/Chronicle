// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_validating;

public class and_the_event_store_is_the_system_event_store : given.a_registration
{
    public static TheoryData<string> ReservedStores() => ["System", "system", "SYSTEM"];

    [Theory]
    [MemberData(nameof(ReservedStores))]
    public async Task should_refuse_the_reserved_store(string store)
    {
        _storage.HasEventStore(store).Returns(true);
        var result = await _scenario.Validate(new RegisterDeployment(store, 10UL, "fingerprint"));

        result.IsValid.ShouldBeFalse();
        result.ValidationResults.ShouldContain(validation =>
            validation.Message == "The System event store is reserved for the kernel and cannot be written through the public API." &&
            validation.Members.Contains("eventStore"));
    }

    [Fact]
    async Task should_not_register_a_deployment_for_the_reserved_store()
    {
        _storage.HasEventStore("System").Returns(true);
        var result = await _scenario.Execute(new RegisterDeployment("System", 10UL, "fingerprint"));

        result.IsSuccess.ShouldBeFalse();
        _grainFactory.DidNotReceive().GetGrain<IEventStoreDeploymentRegistry>("System");
    }
}
