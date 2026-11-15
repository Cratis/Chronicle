// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPortPolicy;

public class when_deciding_about_the_enrollment_routes : Specification
{
    ChronicleOptions _hosted;
    ChronicleOptions _notHosted;
    bool _redeemOnFederationPort;
    bool _renewOnFederationPort;
    bool _authorityOnFederationPort;
    bool _redeemOnMainPort;
    bool _redeemWhenNotHosted;
    bool _redeemWithOtherCasing;
    bool _redeemNeedsCertificate;
    bool _authorityNeedsCertificate;
    bool _renewNeedsCertificate;
    bool _pullNeedsCertificate;
    bool _pullStillServed;

    void Establish()
    {
        _hosted = new() { Federation = new() { Enabled = true, Port = 35100, Hosted = new() { Enabled = true } } };
        _notHosted = new() { Federation = new() { Enabled = true, Port = 35100 } };
    }

    void Because()
    {
        _redeemOnFederationPort = FederationPortPolicy.ShouldReject(_hosted, 35100, FederationEnrollmentRoutes.Redeem);
        _renewOnFederationPort = FederationPortPolicy.ShouldReject(_hosted, 35100, FederationEnrollmentRoutes.Renew);
        _authorityOnFederationPort = FederationPortPolicy.ShouldReject(_hosted, 35100, FederationEnrollmentRoutes.Authority);
        _redeemOnMainPort = FederationPortPolicy.ShouldReject(_hosted, _hosted.Port, FederationEnrollmentRoutes.Redeem);
        _redeemWhenNotHosted = FederationPortPolicy.ShouldReject(_notHosted, 35100, FederationEnrollmentRoutes.Redeem);
        _redeemWithOtherCasing = FederationPortPolicy.ShouldReject(_hosted, 35100, FederationEnrollmentRoutes.Redeem.ToUpperInvariant());
        _redeemNeedsCertificate = FederationPortPolicy.RequiresClientCertificate(FederationEnrollmentRoutes.Redeem);
        _authorityNeedsCertificate = FederationPortPolicy.RequiresClientCertificate(FederationEnrollmentRoutes.Authority);
        _renewNeedsCertificate = FederationPortPolicy.RequiresClientCertificate(FederationEnrollmentRoutes.Renew);
        _pullNeedsCertificate = FederationPortPolicy.RequiresClientCertificate(FederationPortPolicy.ServicePathPrefix + "Pull");
        _pullStillServed = FederationPortPolicy.ShouldReject(_hosted, 35100, FederationPortPolicy.ServicePathPrefix + "Pull");
    }

    [Fact] void should_serve_redeeming_on_the_federation_port() => _redeemOnFederationPort.ShouldBeFalse();
    [Fact] void should_serve_renewing_on_the_federation_port() => _renewOnFederationPort.ShouldBeFalse();
    [Fact] void should_serve_the_authority_certificate_on_the_federation_port() => _authorityOnFederationPort.ShouldBeFalse();
    [Fact] void should_refuse_redeeming_on_the_main_port() => _redeemOnMainPort.ShouldBeTrue();
    [Fact] void should_refuse_redeeming_when_hosted_federation_is_not_enabled() => _redeemWhenNotHosted.ShouldBeTrue();
    [Fact] void should_match_the_route_case_insensitively() => _redeemWithOtherCasing.ShouldBeFalse();
    [Fact] void should_not_require_a_certificate_to_redeem() => _redeemNeedsCertificate.ShouldBeFalse();
    [Fact] void should_not_require_a_certificate_to_fetch_the_public_authority_certificate() => _authorityNeedsCertificate.ShouldBeFalse();
    [Fact] void should_require_a_certificate_to_renew() => _renewNeedsCertificate.ShouldBeTrue();
    [Fact] void should_still_require_a_certificate_for_the_pull_service() => _pullNeedsCertificate.ShouldBeTrue();
    [Fact] void should_still_serve_the_pull_service() => _pullStillServed.ShouldBeFalse();
}
