// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_derived_generation_records_earlier_migrations : given.a_generation_history
{
    void Establish() => ChangeMigrations();

    async Task Because() => _result = await Verify();

    [Fact] void should_use_the_recorded_migrations() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
