// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_protection_comes_from_the_definition_snapshot : given.a_real_release
{
    Task Establish() => StoreProtectedName("Alice");

    async Task Because() => _result = await Create();

    [Fact] void should_claim() => _result.ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_release_and_decrypt_the_value() => _result.Components[0].Value.ShouldEqual("Alice");
}
