// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleFixture;

public class when_selecting_container_databases_for_cleanup : Specification
{
    IEnumerable<string> _result;

    void Because() => _result = ChronicleFixture.GetMongoDBDatabasesToDrop(
        ["admin", "config", "local", "orleans", "ORLEANS_membership", "chronicle", "read_models"],
        string.Empty,
        ["orleans"]);

    [Fact] void should_preserve_system_and_excluded_databases() => _result.ShouldContainOnly("chronicle", "read_models");
}
