// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_historical_generation_matches : given.a_migrated_event
{
    async Task Because() => _result = await Verify();

    [Fact] void should_require_every_migrated_generation_to_match() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
