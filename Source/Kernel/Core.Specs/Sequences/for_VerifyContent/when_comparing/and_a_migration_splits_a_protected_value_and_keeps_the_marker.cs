// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_splits_a_protected_value_and_keeps_the_marker : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        MigrateWith("""{"name":{"$split":{"source":"name","separator":"=","part":0}}}""");
        Store(
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" },
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_trust_a_marker_a_value_transforming_migration_left_intact() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
