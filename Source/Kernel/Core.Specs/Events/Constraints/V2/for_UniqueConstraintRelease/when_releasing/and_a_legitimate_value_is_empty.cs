// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_a_legitimate_value_is_empty : given.a_real_release
{
    ReleasedUniqueConstraintComponents _decrypted;
    ReleasedUniqueConstraintComponents _plain;

    async Task Because()
    {
        await StoreProtectedName(string.Empty);
        _decrypted = await Create();
        SetContent("""{"name":""}""");
        _plain = await Create();
    }

    [Fact] void should_claim_a_value_that_decrypts_to_empty() => _decrypted.ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_keep_the_decrypted_empty_component() => _decrypted.Components[0].Value.ShouldEqual(string.Empty);
    [Fact] void should_claim_a_plain_empty_value() => _plain.ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_keep_the_plain_empty_component() => _plain.Components[0].Value.ShouldEqual(string.Empty);
}
