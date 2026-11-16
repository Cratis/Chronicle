// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_ClusterPeerCertificateValidator.when_validating;

public class and_the_peer_is_issued_by_the_same_authority : given.an_authority_and_certificates
{
    bool _result;

    void Because() => _result = ClusterPeerCertificateValidator.IsTrusted(_local, _issuedByAuthority, [_authority]);

    [Fact] void should_trust_it() => _result.ShouldBeTrue();
}
