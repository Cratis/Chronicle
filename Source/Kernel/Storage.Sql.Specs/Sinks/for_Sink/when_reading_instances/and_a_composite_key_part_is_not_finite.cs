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

public class and_a_composite_key_part_is_not_finite : Specification
{
    CapturingIndexLogger _logger;
    SqlSinkHarness _harness;
    ISink _sink;
    ExpandoObject[] _instances;
    Exception? _error;

    async Task Establish()
    {
        _logger = new CapturingIndexLogger();
        _harness = new SqlSinkHarness { Logger = _logger };
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"number","format":"double"},"second":{"type":"number","format":"float"}}},"name":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("non-finite", "non_finite_composite_keys", "Non-finite", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "non-finite-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            await AddInstance(value, 1f);
            await AddInstance(1d, (float)value);
        }
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        _instances = (await _sink.GetInstances()).Instances.ToArray();
        _ = (await _sink.GetInstances()).Instances.ToArray();
    });

    void Destroy() => _harness.Dispose();

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_return_all_instances() => _instances.Length.ShouldEqual(6);
    [Fact] void should_not_return_any_rebuilt_keys() => _instances.All(instance => !((IDictionary<string, object?>)instance).TryGetValue("id", out var key) || key is not IDictionary<string, object?>).ShouldBeTrue();
    [Fact] void should_preserve_the_other_properties() => _instances.All(instance => ((IDictionary<string, object?>)instance).TryGetValue("name", out var name) && Equals(name, "non-finite")).ShouldBeTrue();
    [Fact] void should_warn_once_for_non_finite_parts() => _logger.Warnings.Single().Contains("a key part is not finite").ShouldBeTrue();

    async Task AddInstance(double first, float second)
    {
        var keyValue = new ExpandoObject();
        ((IDictionary<string, object?>)keyValue)["first"] = first;
        ((IDictionary<string, object?>)keyValue)["second"] = second;
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "non-finite")])]);
        await _sink.ApplyChanges(new Key(keyValue, ArrayIndexers.NoIndexers), changeset, 1UL);
    }
}
