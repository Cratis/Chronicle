// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_request_differs_only_in_a_derived_generation : given.a_generation_history
{
    void Establish() => RequestSecondGeneration("""{"value":42,"detail":"different"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_not_claim_equality_from_the_matching_original_alone() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
