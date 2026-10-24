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

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class when_the_sqlite_decimal_shadow_name_collides : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    string _pagingRefusal;
    string _joinRefusal;
    bool _written;

    void Establish()
    {
        _harness = new SqlSinkHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"amount":{"type":"number","format":"decimal"},"__ORDER_id":{"type":"string"},"__order_amount":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("decimal-collision", "decimal_collision", "Decimal collision", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "decimal-collision", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
    }

    async Task Because()
    {
        var key = new Key(1m, ArrayIndexers.NoIndexers);
        var write = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        write.InitialState.Returns(new ExpandoObject());
        write.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("amount", null, 193.58m)])]);
        (await _sink.ApplyChanges(key, write, 1UL)).ShouldBeEmpty();
        _written = (await _sink.FindOrDefault(key)) is not null;
        try
        {
            await _sink.GetInstances();
            _pagingRefusal = string.Empty;
        }
        catch (InvalidOperationException exception)
        {
            _pagingRefusal = exception.Message;
        }
        var join = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        join.Changes.Returns([new Joined(new ExpandoObject(), 193.580m, "amount", ArrayIndexers.NoIndexers, [])]);
        try
        {
            await _sink.ApplyChanges(key, join, 2UL);
            _joinRefusal = string.Empty;
        }
        catch (InvalidOperationException exception)
        {
            _joinRefusal = exception.Message;
        }
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_allow_normal_writes_and_keyed_reads() => _written.ShouldBeTrue();
    [Fact] void should_refuse_numeric_paging_instead_of_using_text_order() => _pagingRefusal.ShouldContain("__order_id");
    [Fact] void should_refuse_exact_narrowing_instead_of_using_the_schema_property() => _joinRefusal.ShouldContain("__order_amount");
    [Fact] void should_name_the_table_in_the_paging_refusal() => _pagingRefusal.ShouldContain("decimal_collision");
    [Fact] void should_name_the_table_in_the_join_refusal() => _joinRefusal.ShouldContain("decimal_collision");
}
