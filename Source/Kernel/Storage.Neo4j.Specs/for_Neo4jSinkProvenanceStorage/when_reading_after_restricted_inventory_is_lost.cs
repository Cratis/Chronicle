// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage;

public class when_reading_after_restricted_inventory_is_lost : given.a_graph_database
{
    IRecord? _inventory;
    IRecord? _marker;
    SinkProvenanceState _state;

    async Task Establish()
    {
        _inventory = Inventory(Guid.NewGuid());
        _runtime.RunAsync(Read).Returns(_ => Cursor(Records()));
        _runtime.RunAsync(LockInventory, Arg.Any<object>()).Returns(_ => Cursor());
        _runtime.RunAsync(Complete, Arg.Any<object>()).Returns(call =>
        {
            var parameters = call.Arg<object>();
            var id = Guid.Parse(Get<string>(Fields(_inventory!), "_id"));
            _inventory = Inventory(id, Parameter<int>(parameters, "version"), Parameter<bool>(parameters, "restricted"));
            return Cursor();
        });
        _runtime.RunAsync(Mark, Arg.Any<object>()).Returns(_ =>
        {
            _marker ??= Protection();
            return Cursor();
        });
        _runtime.RunAsync(Seed, Arg.Any<object>()).Returns(call =>
        {
            _inventory ??= Inventory(Guid.Parse(Parameter<string>(call.Arg<object>(), "id")));
            return Cursor();
        });
        await _provenance.CompleteInventory(true);
        _inventory = null;
    }

    async Task Because() => _state = await StorageFor(_runtime, _schema, "physical").ReadState();

    [Fact] void should_not_recover_completion() => _state.IsComplete.ShouldBeFalse();
    [Fact] void should_recover_restriction() => _state.Restricted.ShouldBeTrue();

    static T Parameter<T>(object parameters, string name) => (T)parameters.GetType().GetProperty(name)!.GetValue(parameters)!;

    IRecord[] Records() => new[] { _inventory, _marker }.OfType<IRecord>().ToArray();
}
