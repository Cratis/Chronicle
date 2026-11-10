// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Storage.Neo4j;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_ensuring_the_schema.for_a_definition_that_uses_reserved_names.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_ensuring_the_schema;

/// <summary>
/// A graph that names a property like the bookkeeping would collide with it, so it is refused before anything reaches the server.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class for_a_definition_that_uses_reserved_names(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public Exception? ReservedProperty;
        public Exception? ReservedKey;

        async Task Because()
        {
            ReservedProperty = await Catch.Exception(() => CreateSink(Definition("personId", "_watermark")).EnsureSchema());
            ReservedKey = await Catch.Exception(() => CreateSink(Definition("_namespace", "name")).EnsureSchema());
        }

        static GraphDefinition Definition(string key, string property) =>
            new("Reserved", [new("Person", key, GraphDefinitions.Schema((key, "string"), (property, "string")))], []);
    }

    [Fact] void should_refuse_a_property_that_starts_with_the_prefix_of_the_bookkeeping() => Context.ReservedProperty.ShouldBeOfExactType<ReservedGraphPropertyName>();
    [Fact] void should_refuse_a_key_that_starts_with_the_prefix_of_the_bookkeeping() => Context.ReservedKey.ShouldBeOfExactType<ReservedGraphPropertyName>();
}
