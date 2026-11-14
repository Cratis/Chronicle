// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_redeeming_enrollments_that_are_not_valid : given.a_federation_authority
{
    FederationIssuance _unknown;
    FederationIssuance _wrongSecret;
    FederationIssuance _expired;
    FederationIssuance _used;
    FederationIssuance _revoked;
    FederationIssuance _notHosted;

    async Task Because()
    {
        using var key = NewKey();
        var csr = Csr(key);

        _unknown = await _authority.Redeem("0123456789abcdef", "secret", csr);

        var wrongSecret = await Enroll("acme", "one");
        _wrongSecret = await _authority.Redeem(wrongSecret.EnrollmentId, wrongSecret.Secret + "x", csr);

        var used = await Enroll("acme", "two");
        await _authority.Redeem(used.EnrollmentId, used.Secret, csr);
        _used = await _authority.Redeem(used.EnrollmentId, used.Secret, csr);

        var revoked = await Enroll("acme", "three");
        await _authority.RevokeEnrollment(revoked.EnrollmentId);
        _revoked = await _authority.Redeem(revoked.EnrollmentId, revoked.Secret, csr);

        var expired = await Enroll("acme", "four");
        _time.Advance(TimeSpan.FromDays(7));
        _expired = await _authority.Redeem(expired.EnrollmentId, expired.Secret, csr);

        _notHosted = await Create(new() { Federation = new() { Enabled = true } }).Redeem(expired.EnrollmentId, expired.Secret, csr);
    }

    [Fact] void should_refuse_an_unknown_enrollment() => _unknown.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_refuse_a_wrong_secret_with_the_same_answer() => _wrongSecret.ShouldEqual(_unknown);
    [Fact] void should_refuse_a_used_enrollment_with_the_same_answer() => _used.ShouldEqual(_unknown);
    [Fact] void should_refuse_a_revoked_enrollment_with_the_same_answer() => _revoked.ShouldEqual(_unknown);
    [Fact] void should_refuse_an_expired_enrollment_with_the_same_answer() => _expired.ShouldEqual(_unknown);
    [Fact] void should_refuse_when_hosted_federation_is_not_enabled_with_the_same_answer() => _notHosted.ShouldEqual(_unknown);
}
