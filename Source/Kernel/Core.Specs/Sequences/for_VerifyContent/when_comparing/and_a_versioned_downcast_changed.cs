// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_versioned_downcast_changed : given.a_generation_history
{
    void Establish()
    {
        RequestSecondGeneration();
        ChangeMigrations(downcast: """{"value":{"$mapValues":{"source":"value","mappings":[{"from":42,"to":43}]}}}""");
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_reconstruct_the_original_using_the_recorded_migrations() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
