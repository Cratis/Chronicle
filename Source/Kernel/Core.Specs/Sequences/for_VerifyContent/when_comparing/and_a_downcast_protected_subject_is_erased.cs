// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_downcast_protected_subject_is_erased : given.a_token_downcast_from_a_protected_generation
{
    async Task Establish() => await _keys.DeleteFor("store", "tenant", "person");

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_as_equal() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
