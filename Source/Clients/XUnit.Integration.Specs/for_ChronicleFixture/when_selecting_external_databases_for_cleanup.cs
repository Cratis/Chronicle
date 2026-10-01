// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleFixture;

public class when_selecting_external_databases_for_cleanup : Specification
{
    IEnumerable<string> _result;

    void Because() => _result = ChronicleFixture.GetMongoDBDatabasesToDrop(
        ["admin", "config", "local", "run_orleans", "run_ORLEANS_membership", "run_chronicle", "other_chronicle", "RUN_chronicle"],
        "run_",
        ["orleans"]);

    [Fact] void should_only_drop_owned_databases_without_an_excluded_logical_prefix() => _result.ShouldContainOnly("run_chronicle");
}
