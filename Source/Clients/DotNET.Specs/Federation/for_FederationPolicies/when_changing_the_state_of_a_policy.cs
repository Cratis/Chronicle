// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_changing_the_state_of_a_policy : given.policies_on_a_substitute_kernel
{
    SuspendFederationPolicyRequest _suspend;
    ResumeFederationPolicyRequest _resume;
    RevokeFederationPolicyRequest _revoke;
    RevokeFederationPolicyDirectionRequest _revokeDirection;

    void Establish()
    {
        _wire.SuspendFederationPolicy(Arg.Any<SuspendFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _suspend = call.Arg<SuspendFederationPolicyRequest>();
                return new CommandResult();
            });
        _wire.ResumeFederationPolicy(Arg.Any<ResumeFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _resume = call.Arg<ResumeFederationPolicyRequest>();
                return new CommandResult();
            });
        _wire.RevokeFederationPolicy(Arg.Any<RevokeFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _revoke = call.Arg<RevokeFederationPolicyRequest>();
                return new CommandResult();
            });
        _wire.RevokeFederationPolicyDirection(Arg.Any<RevokeFederationPolicyDirectionRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _revokeDirection = call.Arg<RevokeFederationPolicyDirectionRequest>();
                return new CommandResult();
            });
    }

    async Task Because()
    {
        await _policies.Suspend("acme");
        await _policies.Resume("beta");
        await _policies.Revoke("gamma");
        await _policies.RevokeDirection("delta", FederationDirection.AcceptorOutbound);
    }

    [Fact] void should_suspend_the_subject() => _suspend.Subject.ShouldEqual("acme");
    [Fact] void should_resume_the_subject() => _resume.Subject.ShouldEqual("beta");
    [Fact] void should_revoke_the_subject() => _revoke.Subject.ShouldEqual("gamma");
    [Fact] void should_revoke_the_direction_of_the_subject() => (_revokeDirection.Subject, _revokeDirection.Direction).ShouldEqual(("delta", Contracts.Federation.FederationDirection.AcceptorOutbound));
}
