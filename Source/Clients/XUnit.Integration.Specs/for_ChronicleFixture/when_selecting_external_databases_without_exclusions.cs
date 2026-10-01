// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleFixture;

public class when_selecting_external_databases_without_exclusions : Specification
{
    IEnumerable<string> _result;

    void Because() => _result = ChronicleFixture.GetMongoDBDatabasesToDrop(
        ["admin", "config", "local", "run_orleans", "run_chronicle", "other_chronicle"],
        "run_");

    [Fact] void should_drop_all_owned_databases() => _result.ShouldContainOnly("run_orleans", "run_chronicle");
}
