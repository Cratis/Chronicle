// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_another_generation_cannot_be_released : given.a_migrated_protected_event
{
    void Establish()
    {
        _command = _command with { Content = "{\"value\":\"different\"}" };
        _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = _stored.GenerationalContent[1], [2] = "{\"value\":\"not-ciphertext\"}" } };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_return_a_partial_comparison_even_if_one_generation_differs() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
