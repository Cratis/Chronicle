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
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class and_the_provider_is_sqlite : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    IDictionary<string, object?> _read;
    string _storageType;
    readonly Key _key = new("decimal", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"},"precise":{"type":"number","format":"decimal"},"nested":{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("decimal", "decimal_values", "Decimal", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "decimal-projection", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    async Task Because()
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        dynamic nested = new ExpandoObject();
        nested.amount = 1234567890.123456789012345678m;
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [
            new PropertyDifference("amount", null, 193.58m), new PropertyDifference("precise", null, 1234567890.123456789012345678m), new PropertyDifference("nested", null, (ExpandoObject)nested)])]);
        (await _sink.ApplyChanges(_key, changeset, 1UL)).ShouldBeEmpty();
        _read = (await _sink.FindOrDefault(_key))!;
        await using var connection = new SqliteConnection(_harness.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT typeof(amount) FROM decimal_values";
        _storageType = (string)(await command.ExecuteScalarAsync())!;
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_use_text_storage() => _storageType.ShouldEqual("text");
    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)_read["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_read["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
    [Fact] void should_preserve_nested_json_decimals() => decimal.GetBits((decimal)((IDictionary<string, object?>)_read["nested"]!)["amount"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
