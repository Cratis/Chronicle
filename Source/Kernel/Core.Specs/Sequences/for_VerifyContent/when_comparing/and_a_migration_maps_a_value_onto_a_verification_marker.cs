// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_maps_a_value_onto_a_verification_marker : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        // The real upcast writes the literal text into alias. A predictable marker would restore it as the protected
        // name instead, matching a stored event whose alias holds that name.
        MigrateWith("""{"alias":{"$mapValues":{"source":"kind","mappings":[{"from":"x","to":"chronicle-verification:0"}]}}}""");
        Store(
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" },
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x", ["alias"] = Protect("a") });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_restore_a_value_the_migration_synthesized() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
