// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_declared_null_members_are_dropped_by_append : given.a_storage_round_trip
{
    async Task Establish() => await Store("""{"type":"object","properties":{"value":{"type":["string","null"]},"name":{"type":"string"}}}""", """{"value":null,"name":"kept"}""", sql: true);

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_append_shape_without_restoring_dropped_members() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
