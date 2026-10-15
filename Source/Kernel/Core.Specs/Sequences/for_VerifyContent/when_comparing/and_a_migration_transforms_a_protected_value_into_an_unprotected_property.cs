// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migration_transforms_a_protected_value_into_an_unprotected_property : given.a_migrated_event_with_protected_properties
{
    void Establish()
    {
        MigrateWith("""{"note":{"$split":{"source":"name","separator":"=","part":5}}}""");
        Store(
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x" },
            new JsonObject { ["name"] = Protect("a"), ["kind"] = "x", ["note"] = "other" });
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_be_unavailable_rather_than_different() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
