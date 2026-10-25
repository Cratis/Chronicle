// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_reading_instances;

/// <summary>
/// A stored composite key whose parts cannot be told apart is returned without its key, and the sink warns once.
/// </summary>
public class and_the_composite_key_is_ambiguous : Specification
{
    const string ReadModelName = "ambiguous_composite_keys";
    const string KeyPart = "a_b";

    CapturingIndexLogger _logger;
    SqlSinkHarness _harness;
    ISink _sink;
    ExpandoObject[] _instances;
    object? _key;

    async Task Establish()
    {
        _logger = new CapturingIndexLogger();
        _harness = new SqlSinkHarness { Logger = _logger };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"integer","format":"int32"}}},"name":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("ambiguous", ReadModelName, "Ambiguous", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "ambiguous-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));

        var keyValue = new ExpandoObject();
        ((IDictionary<string, object?>)keyValue)["first"] = KeyPart;
        ((IDictionary<string, object?>)keyValue)["second"] = 1;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "ambiguous")])]);
        await _sink.ApplyChanges(new Key(keyValue, ArrayIndexers.NoIndexers), changeset, 1UL);
    }

    async Task Because()
    {
        _instances = (await _sink.GetInstances()).Instances.ToArray();
        _key = ((IDictionary<string, object?>)_instances[0]).TryGetValue("id", out var key) ? key : null;
        _ = (await _sink.GetInstances()).Instances.ToArray();
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_return_the_instance() => _instances.Length.ShouldEqual(1);
    [Fact] void should_not_return_a_rebuilt_key() => (_key is IDictionary<string, object?>).ShouldBeFalse();
    [Fact] void should_warn_once() => _logger.Warnings.Count().ShouldEqual(1);
    [Fact] void should_name_the_table_in_the_warning() => _logger.Warnings.Single().Contains(ReadModelName).ShouldBeTrue();
    [Fact] void should_not_log_the_key_value() => _logger.Warnings.Single().Contains(KeyPart).ShouldBeFalse();
}
