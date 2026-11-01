// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_downcast_generations_inherit_protection : given.a_token_downcast_from_a_protected_generation
{
    async Task Because() => _result = await Verify();

    [Fact] void should_compare_all_generations_as_equal() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
