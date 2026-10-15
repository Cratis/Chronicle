// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_dynamic_and_nested_protected_values_match_across_generations : given.a_migrated_event_with_dynamic_and_nested_protected_values
{
    async Task Establish() => await Store("""{"detail":{"$defaultValue":"default"}}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_generations_protected_by_the_append_pipeline() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
