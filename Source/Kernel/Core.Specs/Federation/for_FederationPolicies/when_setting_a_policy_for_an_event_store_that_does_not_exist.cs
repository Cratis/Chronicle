// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_setting_a_policy_for_an_event_store_that_does_not_exist : given.a_peer_admission
{
    Exception _exception;

    void Establish() => _storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(false);

    async Task Because() => _exception = await Catch.Exception(() => _policies.Set("acme", FederationDirection.AcceptorInbound, "unknown", "ns", true, [], "operator"));

    [Fact] void should_refuse_the_policy() => _exception.ShouldBeOfExactType<InvalidFederationPolicy>();
    [Fact] void should_record_nothing() => _registry.Appended.ShouldBeEmpty();
}
