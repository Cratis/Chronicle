// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using SqlSink = Cratis.Chronicle.Storage.Sql.Sinks.Sink;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

public class and_children_have_nested_identities : Specification
{
    const string Container = "nested_identities";
    readonly Key _key = new("parent", ArrayIndexers.NoIndexers);
    SqliteConnection _connection;
    SqlSink _sink;
    IReadOnlyList<ProjectedColumn> _columns;
    ExpandoObject _updated;
    ExpandoObject _removed;
    ExpandoObject _reintroduced;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"children":{"type":"array","items":{"type":"object","properties":{"identity":{"type":"object","properties":{"id":{"type":"string"},"topic":{"type":"string"}}}}}}}}
            """);
        _columns = ProjectedColumns.ForSchema(schema);
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        await using (var context = CreateContext())
        {
            await context.Database.EnsureCreatedAsync();
        }

        var database = Substitute.For<IDatabase>();
        database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>())
            .Returns(_ => Task.FromResult(new DbContextScope<ReadModelDbContext>(CreateContext(), () => { })));
        var definition = new ReadModelDefinition("nested-identities", "NestedIdentities", Container, ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = new SqlSink("test-store", "test-namespace", definition, database, new ExpandoObjectConverter(new TypeFormats()), new ReplayingTables());
        await Apply(new ChildAdded(Child("A", "First"), "[children]", "identity.id", "A", ArrayIndexers.NoIndexers), 1);
        await Apply(new ChildAdded(Child("B", "Second"), "[children]", "identity.id", "B", ArrayIndexers.NoIndexers), 2);
    }

    async Task Because()
    {
        var indexers = new ArrayIndexers([new ArrayIndexer("[children]", "identity.id", "A")]);
        await Apply(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[children].identity.topic", "First", "Updated", indexers)]), 3);
        _updated = (await _sink.FindOrDefault(_key))!;
        await Apply(new ChildAdded(Child("A", "Reintroduced"), "[children]", "identity.id", "A", ArrayIndexers.NoIndexers), 4);
        _reintroduced = (await _sink.FindOrDefault(_key))!;
        await Apply(new ChildRemoved(Child("A", "Reintroduced"), "[children]", "identity.id", "A"), 5);
        _removed = (await _sink.FindOrDefault(_key))!;
    }

    void Destroy() => _connection.Dispose();

    [Fact] void should_update_without_adding_a_child() => Children(_updated).Length.ShouldEqual(2);
    [Fact] void should_update_the_matching_child() => new PropertyPath("identity.topic").GetValue(Children(_updated)[0], ArrayIndexers.NoIndexers).ShouldEqual("Updated");
    [Fact] void should_upsert_without_adding_a_child() => Children(_reintroduced).Length.ShouldEqual(2);
    [Fact] void should_replace_the_matching_child() => new PropertyPath("identity.topic").GetValue(Children(_reintroduced)[0], ArrayIndexers.NoIndexers).ShouldEqual("Reintroduced");
    [Fact] void should_remove_the_matching_child() => Children(_removed).Length.ShouldEqual(1);
    [Fact] void should_preserve_the_other_child() => new PropertyPath("identity.id").GetValue(Children(_removed)[0], ArrayIndexers.NoIndexers).ShouldEqual("B");

    async Task Apply(Change change, ulong sequenceNumber)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([change]);
        (await _sink.ApplyChanges(_key, changeset, sequenceNumber)).ShouldBeEmpty();
    }

    ReadModelDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(_connection).AddConceptAsSupport().Options,
        Container,
        _columns,
        Substitute.For<IReadModelMigrator>());

    static ExpandoObject Child(string id, string topic)
    {
        dynamic child = new ExpandoObject();
        child.identity = new ExpandoObject();
        child.identity.id = id;
        child.identity.topic = topic;
        return child;
    }

    static ExpandoObject[] Children(ExpandoObject state) => ((IEnumerable<object>)((IDictionary<string, object?>)state)["children"]!).Cast<ExpandoObject>().ToArray();
}
