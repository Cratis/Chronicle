// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_downcast_discarded_the_only_difference : given.a_migrated_event
{
    async Task Establish() => await Append("""{"value":42,"detail":"different"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_not_recognize_the_matching_downcast_as_a_duplicate() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
