// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_recorded_reconstruction_of_the_original_differs : given.a_generation_history
{
    void Establish()
    {
        ChangeMigrations(downcast: """{"value":{"$mapValues":{"source":"value","mappings":[{"from":43,"to":42}]}}}""");
        RequestSecondGeneration("""{"value":43,"detail":"default"}""");
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_prove_the_difference_with_recorded_not_current_migrations() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
