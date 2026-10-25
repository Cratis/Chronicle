// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

/// <summary>
/// A SQL read model whose key is a composite of two parts, over a database where the tables can be made legacy tables.
/// </summary>
public class a_composite_keyed_sql_read_model : Specification
{
    protected SqlSinkHarness _harness;
    protected ISink _sink;

    /// <summary>
    /// Gets the namespace the sink is created for.
    /// </summary>
    protected virtual EventStoreNamespaceName Namespace => "test-namespace";

    void Establish()
    {
        _harness = new SqlSinkHarness { Namespace = Namespace };
        _sink = _harness.CreateSink(ReadModel());
    }

    void Destroy() => _harness.Dispose();

    /// <summary>
    /// Creates a composite key.
    /// </summary>
    /// <param name="first">The first key part.</param>
    /// <param name="second">The second key part.</param>
    /// <returns>The key.</returns>
    protected static Key CompositeKey(string first, string second)
    {
        var value = new ExpandoObject();
        ((IDictionary<string, object?>)value)["first"] = first;
        ((IDictionary<string, object?>)value)["second"] = second;
        return new Key(value, ArrayIndexers.NoIndexers);
    }

    /// <summary>
    /// Creates a changeset setting the name property.
    /// </summary>
    /// <param name="name">The name to set.</param>
    /// <returns>The changeset.</returns>
    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetNamed(string name)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, name)])]);
        return changeset;
    }

    /// <summary>
    /// Makes the existing tables legacy tables and returns a sink that has not cached anything about them.
    /// </summary>
    /// <returns>The new sink.</returns>
    protected async Task<ISink> ReopenAsLegacy()
    {
        await _sink.GetInstances();
        await _harness.MakeTablesLegacy();
        return _harness.CreateSinkForTheSameReadModel();
    }

    static ReadModelDefinition ReadModel() => new(
        "composite",
        "composite_keys",
        "Composite",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "composite-projection",
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}},"name":{"type":"string"}}}""")
        },
        []);
}
