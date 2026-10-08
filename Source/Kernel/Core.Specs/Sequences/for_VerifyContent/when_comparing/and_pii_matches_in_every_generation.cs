// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_pii_matches_in_every_generation : given.a_migrated_protected_event
{
    async Task Because() => _result = await Verify();

    [Fact] void should_restore_masked_values_after_migration() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
