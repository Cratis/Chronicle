// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_SchemaCommand;

public class when_parsing_schema_arguments : Specification
{
    SchemaCommand _command;

    void Because() => _command = SchemaCommand.Parse(["--migrate-only", "--event-store", "orders", "--namespace", "production", "--event-store", "billing", "--urls", "http://localhost:8080"]);

    [Fact] void should_select_the_action() => _command.Action.ShouldEqual(SchemaAction.MigrateAndExit);
    [Fact] void should_collect_repeatable_stores() => _command.Scope.EventStores.ShouldContainOnly("orders", "billing");
    [Fact] void should_collect_namespaces() => _command.Scope.Namespaces.ShouldContainOnly("production");
    [Fact] void should_leave_only_host_arguments() => _command.RemainingArgs.ShouldContainOnly("--urls", "http://localhost:8080");
}
