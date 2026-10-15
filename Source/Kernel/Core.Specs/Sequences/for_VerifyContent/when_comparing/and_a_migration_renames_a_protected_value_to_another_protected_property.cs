// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_renames_a_protected_value_to_another_protected_property : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        MigrateWith("""{"alias":{"$rename":"name"},"kind":"kind"}""");
        Store(
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" },
            new JsonObject { ["kind"] = "x", ["alias"] = Protect("a") });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_keep_the_value_opaque_and_compare_equal() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
