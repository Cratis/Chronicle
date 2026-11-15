// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_setting_policies_that_are_not_valid : given.a_peer_admission
{
    [Theory]
    [InlineData("", "store", "ns", true)]
    [InlineData("a/b", "store", "ns", true)]
    [InlineData("acme", "", "ns", true)]
    [InlineData("acme", "store", "", true)]
    [InlineData("acme", "store", "ns", false)]
    public async Task should_refuse_and_record_nothing(string subject, string eventStore, string @namespace, bool allPublic)
    {
        var exception = await Catch.Exception(() => _policies.Set(subject, FederationDirection.AcceptorOutbound, eventStore, @namespace, allPublic, [], "operator"));
        exception.ShouldBeOfExactType<InvalidFederationPolicy>();
        _registry.Appended.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(FederationDirection.AcceptorInbound)]
    [InlineData(FederationDirection.AcceptorOutbound)]
    public async Task should_refuse_the_system_event_store_as_the_target(FederationDirection direction)
    {
        var exception = await Catch.Exception(() => _policies.Set("acme", direction, "System", "ns", true, [], "operator"));
        exception.ShouldBeOfExactType<InvalidFederationPolicy>();
        _registry.Appended.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_an_unknown_direction()
    {
        var exception = await Catch.Exception(() => _policies.Set("acme", (FederationDirection)99, "store", "ns", true, [], "operator"));
        exception.ShouldBeOfExactType<InvalidFederationPolicy>();
    }

    [Fact]
    public async Task should_refuse_to_change_a_subject_without_a_policy()
    {
        var suspend = await Catch.Exception(() => _policies.Suspend("nobody", "operator"));
        var resume = await Catch.Exception(() => _policies.Resume("nobody", "operator"));
        var revoke = await Catch.Exception(() => _policies.Revoke("nobody", "operator"));
        suspend.ShouldBeOfExactType<InvalidFederationPolicy>();
        resume.ShouldBeOfExactType<InvalidFederationPolicy>();
        revoke.ShouldBeOfExactType<InvalidFederationPolicy>();
    }
}
