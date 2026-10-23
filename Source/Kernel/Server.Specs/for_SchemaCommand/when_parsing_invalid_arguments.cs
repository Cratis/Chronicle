// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_SchemaCommand;

public class when_parsing_invalid_arguments : Specification
{
    [Fact] void should_reject_conflicting_actions() => Catch.Exception(() => SchemaCommand.Parse(["--migrate-only", "--verify-schema"])).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_a_missing_store() => Catch.Exception(() => SchemaCommand.Parse(["--event-store"])).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_a_missing_namespace() => Catch.Exception(() => SchemaCommand.Parse(["--namespace", "--verify-schema"])).ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_reject_a_namespace_without_a_store() => Catch.Exception(() => SchemaCommand.Parse(["--namespace", "production"])).ShouldBeOfExactType<ArgumentException>();
}
