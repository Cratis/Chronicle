// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_an_appended_lower_generation_inherits_protection_from_an_unstored_generation : given.a_token_downcast_from_a_protected_generation
{
    void Establish()
    {
        // Without migrations only the appended generation is stored, yet append protected it with the
        // classification inherited from the registered higher generation.
        _stored = _stored with
        {
            Context = _stored.Context with { EventType = new("event", 1), AppendedGeneration = 1 },
            GenerationalContent = new Dictionary<int, string> { [1] = _stored.GenerationalContent[3] }
        };
        _command = _command with { EventType = new("event", 1, false) };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_release_with_the_inherited_protection() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
