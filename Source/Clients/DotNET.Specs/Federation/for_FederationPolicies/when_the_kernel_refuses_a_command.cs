// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_the_kernel_refuses_a_command : given.policies_on_a_substitute_kernel
{
    Exception _set;
    Exception _suspend;
    Exception _resume;
    Exception _revoke;
    Exception _revokeDirection;
    Exception _unauthorized;

    void Establish()
    {
        var refused = new CommandResult { ExceptionMessages = ["the policy is revoked"] };
        _wire.SetFederationPolicy(Arg.Any<SetFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(refused);
        _wire.SuspendFederationPolicy(Arg.Any<SuspendFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(refused);
        _wire.ResumeFederationPolicy(Arg.Any<ResumeFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(refused);
        _wire.RevokeFederationPolicy(Arg.Any<RevokeFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(refused);
        _wire.RevokeFederationPolicyDirection(Arg.Any<RevokeFederationPolicyDirectionRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(refused);
    }

    async Task Because()
    {
        _set = await Catch.Exception(() => _policies.Set("acme", new(FederationDirection.AcceptorOutbound, "store", "acme", true, [])));
        _suspend = await Catch.Exception(() => _policies.Suspend("acme"));
        _resume = await Catch.Exception(() => _policies.Resume("acme"));
        _revoke = await Catch.Exception(() => _policies.Revoke("acme"));
        _revokeDirection = await Catch.Exception(() => _policies.RevokeDirection("acme", FederationDirection.AcceptorInbound));

        _wire.SuspendFederationPolicy(Arg.Any<SuspendFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new CommandResult { IsAuthorized = false });
        _unauthorized = await Catch.Exception(() => _policies.Suspend("acme"));
    }

    [Fact] void should_throw_when_setting_is_refused() => _set.ShouldNotBeNull();
    [Fact] void should_throw_when_suspending_is_refused() => _suspend.ShouldNotBeNull();
    [Fact] void should_throw_when_resuming_is_refused() => _resume.ShouldNotBeNull();
    [Fact] void should_throw_when_revoking_is_refused() => _revoke.ShouldNotBeNull();
    [Fact] void should_throw_when_revoking_a_direction_is_refused() => _revokeDirection.ShouldNotBeNull();
    [Fact] void should_throw_when_the_caller_is_not_authorized() => _unauthorized.ShouldNotBeNull();
}
