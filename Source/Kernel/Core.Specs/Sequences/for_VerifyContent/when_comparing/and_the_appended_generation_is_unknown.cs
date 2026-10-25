// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_appended_generation_is_unknown : given.a_generation_history
{
    void Establish()
    {
        ForgetProvenance();
        _history = _history with { AppendedGeneration = null };
        _stored = _stored with { Context = _stored.Context with { AppendedGeneration = null } };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_infer_it_from_the_highest_stored_generation() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
