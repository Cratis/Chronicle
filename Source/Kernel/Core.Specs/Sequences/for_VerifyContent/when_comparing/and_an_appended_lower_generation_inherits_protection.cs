// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_an_appended_lower_generation_inherits_protection : given.a_token_downcast_from_a_protected_generation
{
    void Establish()
    {
        _stored = _stored with
        {
            Context = _stored.Context with { EventType = new("event", 1), AppendedGeneration = 1 }
        };
        _command = _command with { EventType = new("event", 1, false) };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_strictly_released_appended_copy() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
