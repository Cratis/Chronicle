// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_recorded_migration_version_is_missing : given.a_generation_history
{
    void Establish() => _versions.Clear();

    async Task Because() => _result = await Verify();

    [Fact] void should_not_claim_equality_from_current_migrations() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
