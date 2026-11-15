// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;

namespace Cratis.Chronicle.Federation.for_FederationPolicies;

public class when_getting_link_status : given.policies_on_a_substitute_kernel
{
    static readonly DateTimeOffset _pulled = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    static readonly DateTimeOffset _received = new(2026, 1, 2, 3, 9, 5, TimeSpan.Zero);

    FederationLinkInformation[] _all;
    FederationLinkInformation _connected;
    FederationLinkInformation _quiet;
    FederationLinkInformation _unknown;

    void Establish() =>
        _wire.AllSubjectLinks(Arg.Any<ProtoBuf.Grpc.CallContext>())
            .Returns(new QueryResult<IEnumerable<FederationSubjectLinkResponse>>
            {
                Data =
                [
                    new()
                    {
                        Subject = "acme",
                        Connected = true,
                        LastActivity = new() { Value = _received.ToString("O") },
                        OutboundLastPull = new() { Value = _pulled.ToString("O") },
                        OutboundEvents = 7,
                        OutboundBytes = 700,
                        OutboundThrottled = 2,
                        InboundState = "Backoff",
                        InboundLastReceived = new() { Value = _received.ToString("O") },
                        InboundEvents = 3,
                        InboundBytes = 300,
                        InboundLag = 15,
                        LastError = "The dialer throttled the inbound direction"
                    },
                    new() { Subject = "quiet" }
                ]
            });

    async Task Because()
    {
        _all = [.. await _policies.GetLinks()];
        _connected = await _policies.GetLink("acme");
        _quiet = await _policies.GetLink("quiet");
        _unknown = await _policies.GetLink("nobody");
    }

    [Fact] void should_return_a_link_for_each_subject() => _all.Length.ShouldEqual(2);
    [Fact] void should_report_the_dialer_session_as_open() => _connected.Connected.ShouldBeTrue();
    [Fact] void should_report_the_last_activity() => _connected.LastActivity.ShouldEqual(_received);
    [Fact] void should_report_the_outbound_counters() => (_connected.Outbound.LastActivity, _connected.Outbound.Events, _connected.Outbound.Bytes, _connected.Throttled).ShouldEqual((_pulled, 7UL, 700UL, 2UL));
    [Fact] void should_not_know_the_outbound_lag() => _connected.Outbound.Lag.ShouldBeNull();
    [Fact] void should_report_the_inbound_counters() => (_connected.Inbound.LastActivity, _connected.Inbound.Events, _connected.Inbound.Bytes).ShouldEqual((_received, 3UL, 300UL));
    [Fact] void should_report_the_inbound_lag() => _connected.Inbound.Lag.ShouldEqual(15UL);
    [Fact] void should_report_the_inbound_state_and_last_error() => (_connected.InboundState, _connected.LastError).ShouldEqual(("Backoff", "The dialer throttled the inbound direction"));
    [Fact] void should_report_not_connected_for_a_quiet_subject() => _quiet.Connected.ShouldBeFalse();
    [Fact] void should_report_no_activity_for_a_quiet_subject() => (_quiet.LastActivity, _quiet.Outbound.LastActivity, _quiet.Inbound.LastActivity).ShouldEqual((null, null, null));
    [Fact] void should_not_know_the_inbound_lag_without_a_recorded_state() => _quiet.Inbound.Lag.ShouldBeNull();
    [Fact] void should_return_nothing_for_a_subject_without_a_policy() => _unknown.ShouldBeNull();
}
