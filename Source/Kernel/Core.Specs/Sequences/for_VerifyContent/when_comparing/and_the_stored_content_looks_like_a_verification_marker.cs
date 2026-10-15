// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_stored_content_looks_like_a_verification_marker : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        MigrateWith("{}");
        Store(
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "chronicle-verification:0" },
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_stored_content_it_cannot_tell_apart_from_a_marker() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
