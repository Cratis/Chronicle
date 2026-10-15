// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_moves_a_protected_value_out_of_protection : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        MigrateWith("""{"note":{"$rename":"name"}}""");
        Store(
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" },
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x", ["note"] = "a" });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_a_marker_outside_a_protected_location() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
