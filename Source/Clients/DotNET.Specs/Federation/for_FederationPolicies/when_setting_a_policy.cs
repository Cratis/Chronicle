// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_setting_a_policy : given.policies_on_a_substitute_kernel
{
    SetFederationPolicyRequest _request;

    void Establish() =>
        _wire.SetFederationPolicy(Arg.Any<SetFederationPolicyRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(call =>
            {
                _request = call.Arg<SetFederationPolicyRequest>();
                return new CommandResult();
            });

    Task Because() => _policies.Set("acme", new(FederationDirection.AcceptorInbound, "customer-store", "acme", false, ["OrderPlaced", "OrderShipped"]));

    [Fact] void should_send_the_subject() => _request.Subject.ShouldEqual("acme");
    [Fact] void should_send_the_direction() => _request.Direction.ShouldEqual(Contracts.Federation.FederationDirection.AcceptorInbound);
    [Fact] void should_send_the_target_event_store_and_namespace() => (_request.EventStore, _request.Namespace).ShouldEqual(("customer-store", "acme"));
    [Fact] void should_send_that_not_all_public_event_types_are_allowed() => _request.AllPublic.ShouldBeFalse();
    [Fact] void should_send_the_allowed_event_types() => string.Join(',', _request.EventTypes).ShouldEqual("OrderPlaced,OrderShipped");
}
