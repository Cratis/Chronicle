// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_another_generation_has_no_migration : given.a_migrated_event
{
    void Establish() => _definition = _definition with { Migrations = [] };

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_only_the_requested_generation() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
