// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionChangeRows;

public class when_encoding_identity : Specification
{
    readonly ObserverDefinitionChangeRowKey _key = new(new("observer", "store", "namespace", "event-log"), new(1), "row");

    [Fact] void should_keep_identity_independent_of_current_culture()
    {
        var row = Continuation();
        row = row with { Data = row.Data with { Partition = new Key(12.5m, new ArrayIndexers([new("items", "id", 3.5m)])) } };
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var identity = ObserverDefinitionChangeRows.Identity(row);
            var commaCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            commaCulture.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = commaCulture;
            ObserverDefinitionChangeRows.Identity(row).ShouldEqual(identity);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
    [Fact] void should_distinguish_a_null_plan_partition_from_an_empty_partition()
    {
        var row = Row(new ObserverRestartPlan(Guid.Empty, null, new(1), new(100), ObserverReaderScope.Wildcard));
        ObserverDefinitionChangeRows.Identity(row)
            .ShouldNotEqual(ObserverDefinitionChangeRows.Identity(row with { Data = row.Data with { Partition = "" } }));
    }
    [Fact] void should_include_array_indexer_paths_in_identity()
    {
        var row = Continuation();
        var first = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "a")])) } };
        var second = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("other", "id", "a")])) } };
        ObserverDefinitionChangeRows.Identity(first).ShouldNotEqual(ObserverDefinitionChangeRows.Identity(second));
    }
    [Fact] void should_include_array_indexer_identifiers_in_identity()
    {
        var row = Continuation();
        var first = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "a")])) } };
        var second = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "b")])) } };
        ObserverDefinitionChangeRows.Identity(first).ShouldNotEqual(ObserverDefinitionChangeRows.Identity(second));
    }
    [Fact] void should_include_array_indexer_identifier_paths_in_identity()
    {
        var row = Continuation();
        var first = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "a")])) } };
        var second = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "other", "a")])) } };
        ObserverDefinitionChangeRows.Identity(first).ShouldNotEqual(ObserverDefinitionChangeRows.Identity(second));
    }
    [Fact] void should_hash_raw_utf8_observer_components() => ObserverDefinitionChangeRows.Observer(new("é<\"\\", "store", "namespace", "event-log")).ShouldEqual("101EEE4AC97FC0C98C239DE5D360545D1AFCEF4BEE6ED94ED3F95162E1827AD6");
    [Fact] void should_hash_raw_utf8_partition_components()
    {
        var row = Continuation();
        ObserverDefinitionChangeRows.Identity(row with { Data = row.Data with { Partition = "é<\"\\" } }).ShouldEqual("2D83C62C218826B2877D09597E13EC7E5A8BF5B4E6C1C29B384F35466E087244");
    }
    [Fact] void should_distinguish_component_boundaries() => ObserverDefinitionChangeRows.Observer(new("a|1:b", "c", "namespace", "event-log"))
        .ShouldNotEqual(ObserverDefinitionChangeRows.Observer(new("a", "b|1:c", "namespace", "event-log")));
    [Fact] void should_distinguish_a_null_job_origin_from_an_empty_origin() => ObserverDefinitionChangeRows.Identity(Row(new ObserverJobInventory(Guid.Empty, "job", ObserverDefinitionChangeJobKind.CatchUp, null)))
        .ShouldNotEqual(ObserverDefinitionChangeRows.Identity(Row(new ObserverJobInventory(Guid.Empty, "job", ObserverDefinitionChangeJobKind.CatchUp, new(false, "")))));

    ObserverDefinitionChangeRow<T> Row<T>(T data) => new(_key, 0, ObserverDefinitionChangeRowState.Intended, data, new(10));
    ObserverDefinitionChangeRow<ObserverContinuation> Continuation() => Row(new ObserverContinuation("partition", Guid.Empty));
}
