// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_getting_policies : given.policies_on_a_substitute_kernel
{
    FederationPolicyInformation[] _result;
    Exception _failure;

    void Establish() =>
        _wire.AllPolicies(Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new QueryResult<IEnumerable<FederationPolicyResponse>>
            {
                Data =
                [
                    new()
                    {
                        Subject = "acme",
                        Status = Contracts.Federation.FederationPolicyStatus.Suspended,
                        ChangedBy = "admin",
                        Entries =
                        [
                            new()
                            {
                                Direction = Contracts.Federation.FederationDirection.AcceptorInbound,
                                EventStore = "customer-store",
                                Namespace = "acme",
                                AllPublic = false,
                                EventTypes = ["OrderPlaced"]
                            }
                        ]
                    }
                ]
            });

    async Task Because()
    {
        _result = [.. await _policies.GetPolicies()];

        _wire.AllPolicies(Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(new QueryResult<IEnumerable<FederationPolicyResponse>> { IsAuthorized = false });
        _failure = await Catch.Exception(() => _policies.GetPolicies());
    }

    [Fact] void should_return_the_policy_of_the_subject() => (_result.Length, _result[0].Subject, _result[0].ChangedBy).ShouldEqual((1, "acme", "admin"));
    [Fact] void should_map_the_status() => _result[0].Status.ShouldEqual(FederationPolicyStatus.Suspended);
    [Fact] void should_map_the_entry_direction() => _result[0].Entries[0].Direction.ShouldEqual(FederationDirection.AcceptorInbound);
    [Fact] void should_map_the_entry_target() => (_result[0].Entries[0].EventStore, _result[0].Entries[0].Namespace).ShouldEqual(("customer-store", "acme"));
    [Fact] void should_map_the_entry_event_types() => (_result[0].Entries[0].AllPublic, string.Join(',', _result[0].Entries[0].EventTypes)).ShouldEqual((false, "OrderPlaced"));
    [Fact] void should_throw_when_the_caller_is_not_authorized() => _failure.ShouldNotBeNull();
}
