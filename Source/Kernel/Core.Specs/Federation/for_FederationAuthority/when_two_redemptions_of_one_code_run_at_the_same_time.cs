// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_two_redemptions_of_one_code_run_at_the_same_time : given.a_federation_authority
{
    FederationIssuance[] _results;

    async Task Because()
    {
        var code = await Enroll();

        // Both read the enrollment as unredeemed before either of them has claimed it, so only the claim can tell them apart.
        var arrived = 0;
        var bothRead = new TaskCompletionSource();
        _registry.OnFindEnrollment = () =>
        {
            if (Interlocked.Increment(ref arrived) >= 2)
            {
                bothRead.TrySetResult();
            }

            return bothRead.Task;
        };

        using var firstKey = NewKey();
        using var secondKey = NewKey();
        _results = await Task.WhenAll(
            _authority.Redeem(code.EnrollmentId, code.Secret, Csr(firstKey)),
            _authority.Redeem(code.EnrollmentId, code.Secret, Csr(secondKey)));
    }

    [Fact] void should_issue_to_exactly_one() => _results.Count(_ => _.Outcome == FederationIssuanceOutcome.Issued).ShouldEqual(1);
    [Fact] void should_refuse_the_other() => _results.Count(_ => _.Outcome != FederationIssuanceOutcome.Issued).ShouldEqual(1);
    [Fact] void should_have_issued_exactly_one_certificate() => _registry.Appended.Count(_ => _.Event is FederationCertificateIssued).ShouldEqual(1);
    [Fact] void should_have_recorded_exactly_one_redemption() => _registry.Appended.Count(_ => _.Event is FederationEnrollmentRedeemed).ShouldEqual(1);
}
