// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationSubjectLink;

public class when_reading_the_links_of_subjects : Specification
{
    static readonly DateTimeOffset _received = new(2026, 1, 2, 3, 9, 5, TimeSpan.Zero);

    FederationStatus _status;
    IFederationCertificateRegistry _registry;
    FederationSubjectLink[] _links;

    void Establish()
    {
        _status = new();
        _status.RecordPull("subject:acme", 7, 700);
        _status.RecordThrottled("subject:acme");
        _status.RecordInbound("hosted-acme", 3, 300);
        _status.RecordSessionOpened("acme");
        _status.SetBinding("hosted-acme", new("Backoff", 10, 25, _received, "The dialer throttled the inbound direction"));

        _registry = Substitute.For<IFederationCertificateRegistry>();
        _registry.GetPolicies().Returns(Task.FromResult<IReadOnlyList<FederationPolicyState>>([
            new FederationPolicyState("acme", FederationPolicyStatus.Active, [], "admin"),
            new FederationPolicyState("idle", FederationPolicyStatus.Active, [], "admin")
        ]));
    }

    async Task Because() => _links = [.. await FederationSubjectLink.AllSubjectLinks(_registry, _status)];

    [Fact] void should_report_a_link_per_subject_with_a_policy() => string.Join(',', _links.Select(_ => _.Subject)).ShouldEqual("acme,idle");
    [Fact] void should_report_the_session_as_open() => _links[0].Connected.ShouldBeTrue();
    [Fact] void should_report_the_outbound_traffic() => (_links[0].OutboundEvents, _links[0].OutboundBytes, _links[0].OutboundThrottled).ShouldEqual((7UL, 700UL, 1UL));
    [Fact] void should_report_the_inbound_traffic() => (_links[0].InboundEvents, _links[0].InboundBytes).ShouldEqual((3UL, 300UL));
    [Fact] void should_report_the_inbound_lag() => _links[0].InboundLag.ShouldEqual(15UL);
    [Fact] void should_report_the_inbound_state() => _links[0].InboundState.ShouldEqual("Backoff");
    [Fact] void should_report_the_last_error() => _links[0].LastError.ShouldEqual("The dialer throttled the inbound direction");
    [Fact] void should_report_not_connected_without_a_session() => _links[1].Connected.ShouldBeFalse();
    [Fact] void should_report_no_activity_without_traffic() => _links[1].LastActivity.ShouldBeNull();
    [Fact] void should_report_empty_state_without_a_binding() => (_links[1].InboundState, _links[1].LastError).ShouldEqual((string.Empty, string.Empty));
}
