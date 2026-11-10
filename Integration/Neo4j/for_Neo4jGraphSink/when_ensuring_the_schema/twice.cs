// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_ensuring_the_schema.twice.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_ensuring_the_schema;

/// <summary>
/// Ensuring the schema creates the constraints and the index the graph needs on the server, and ensuring it again is harmless.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class twice(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public Exception? Error;
        public IReadOnlyList<IRecord> Constraints = default!;
        public IReadOnlyList<IRecord> Indexes = default!;
        public Exception? DuplicateKey;
        public Exception? DuplicateKeyInAnotherNamespace;

        async Task Because()
        {
            var sink = CreateSink();
            await sink.EnsureSchema();
            Error = await Catch.Exception(sink.EnsureSchema);
            Constraints = await Read("SHOW CONSTRAINTS YIELD name, type, labelsOrTypes, properties WHERE name STARTS WITH 'chronicle_' RETURN name, type, labelsOrTypes, properties ORDER BY name");
            Indexes = await Read("SHOW INDEXES YIELD name, type, labelsOrTypes, properties WHERE name = 'chronicle_edge_VISITED_visitId' RETURN name, type, labelsOrTypes, properties");

            const string create = "CREATE (:Person {personId: 'duplicate', `_namespace`: $namespace})";
            await Read(create, Parameters("this"));
            DuplicateKey = await Catch.Exception(() => Read(create, Parameters("this")));
            DuplicateKeyInAnotherNamespace = await Catch.Exception(() => Read(create, Parameters("another")));
        }

        Dictionary<string, object> Parameters(string @namespace) => new() { ["namespace"] = @namespace };

        IRecord Constraint(string name) => Constraints.Single(_ => _["name"].As<string>() == name);
        static readonly string[] _ownLabelsAndTypes = ["Person", "Group", "VISITED", "MEMBER_OF"];

        /// <summary>
        /// Gets the names of the constraints belonging to this schema. The server is shared by the specs in the collection,
        /// so other definitions may have left their own constraints; only the owner constraint and those on this graph's
        /// labels and types belong to this schema.
        /// </summary>
        public IReadOnlyList<string> ConstraintNames => [.. Constraints
            .Where(_ => _["name"].As<string>() == "chronicle_owner" ||
                        _["labelsOrTypes"].As<List<object>>().Cast<string>().Any(_ownLabelsAndTypes.Contains))
            .Select(_ => _["name"].As<string>())];
        public string[] PropertiesOf(string name) => [.. Constraint(name)["properties"].As<List<object>>().Cast<string>()];
        public string TypeOf(string name) => Constraint(name)["type"].As<string>();
    }

    [Fact] void should_not_fail_when_the_schema_is_already_there() => Context.Error.ShouldBeNull();
    [Fact] void should_create_a_constraint_for_every_node_label_and_for_the_owners() => Context.ConstraintNames.ShouldContainOnly("chronicle_node_Group_groupId", "chronicle_node_Person_personId", "chronicle_owner");
    [Fact] void should_make_the_constraints_uniqueness_constraints() => Context.ConstraintNames.All(_ => Context.TypeOf(_) == "UNIQUENESS").ShouldBeTrue();
    [Fact] void should_make_the_key_unique_with_the_namespace_when_namespaces_share_the_database() => Context.PropertiesOf("chronicle_node_Person_personId").ShouldContainOnly(WithTheNamespaceWhenShared("personId"));
    [Fact] void should_make_the_owner_unique_with_the_namespace_when_namespaces_share_the_database() => Context.PropertiesOf("chronicle_owner").ShouldContainOnly(WithTheNamespaceWhenShared("_owner"));
    string[] WithTheNamespaceWhenShared(string property) => Context.IsSingleDatabase ? [property, "_namespace"] : [property];
    [Fact] void should_index_the_identifying_property_of_the_edge() => Context.Indexes.Count.ShouldEqual(1);
    [Fact] void should_not_index_an_edge_without_an_identifying_property() => Context.Indexes.Count(_ => _["labelsOrTypes"].As<List<object>>().Contains("MEMBER_OF")).ShouldEqual(0);
    [Fact] void should_enforce_the_uniqueness_of_a_key() => Context.DuplicateKey.ShouldNotBeNull();
    [Fact] void should_enforce_the_uniqueness_within_the_scope_of_the_constraint() => (Context.DuplicateKeyInAnotherNamespace is null).ShouldEqual(Context.IsSingleDatabase);
}
