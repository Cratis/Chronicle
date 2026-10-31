// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_only_an_unversioned_derived_generation_differs : given.a_generation_history
{
    void Establish()
    {
        ForgetProvenance();
        ChangeMigrations();
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_flag_the_appended_original() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
