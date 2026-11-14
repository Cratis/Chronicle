// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_redeeming_with_a_certificate_request_that_cannot_be_honored : given.a_federation_authority
{
    FederationIssuance _garbage;
    FederationIssuance _weakKey;
    FederationIssuance _afterwards;

    async Task Because()
    {
        var code = await Enroll();

        _garbage = await _authority.Redeem(code.EnrollmentId, code.Secret, [1, 2, 3, 4]);

        using var weak = RSA.Create(1024);
        var weakRequest = new CertificateRequest("CN=weak", weak, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSigningRequest();
        _weakKey = await _authority.Redeem(code.EnrollmentId, code.Secret, weakRequest);

        using var key = NewKey();
        _afterwards = await _authority.Redeem(code.EnrollmentId, code.Secret, Csr(key));
    }

    [Fact] void should_say_the_request_is_invalid_for_garbage() => _garbage.Outcome.ShouldEqual(FederationIssuanceOutcome.InvalidRequest);
    [Fact] void should_say_the_request_is_invalid_for_a_weak_key() => _weakKey.Outcome.ShouldEqual(FederationIssuanceOutcome.InvalidRequest);
    [Fact] void should_not_use_up_the_enrollment() => _afterwards.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
}
