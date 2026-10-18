// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Observation;

public abstract partial class DefinitionChangeRowsContract
{
    [Fact] public void should_keep_identity_independent_of_current_culture()
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
    [Fact] public void should_distinguish_a_null_plan_partition_from_an_empty_partition()
    {
        var row = Plan();
        ObserverDefinitionChangeRows.Identity(row with { Data = row.Data with { Partition = null } })
            .ShouldNotEqual(ObserverDefinitionChangeRows.Identity(row with { Data = row.Data with { Partition = "" } }));
    }
    [Fact] public void should_include_array_indexer_paths_in_identity()
    {
        var row = Continuation();
        var first = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "a")])) } };
        var second = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("other", "id", "a")])) } };
        ObserverDefinitionChangeRows.Identity(first).ShouldNotEqual(ObserverDefinitionChangeRows.Identity(second));
    }
    [Fact] public void should_include_array_indexer_identifiers_in_identity()
    {
        var row = Continuation();
        var first = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "a")])) } };
        var second = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "b")])) } };
        ObserverDefinitionChangeRows.Identity(first).ShouldNotEqual(ObserverDefinitionChangeRows.Identity(second));
    }
    [Fact] public void should_include_array_indexer_identifier_paths_in_identity()
    {
        var row = Continuation();
        var first = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "id", "a")])) } };
        var second = row with { Data = row.Data with { Partition = new Key("partition", new ArrayIndexers([new("items", "other", "a")])) } };
        ObserverDefinitionChangeRows.Identity(first).ShouldNotEqual(ObserverDefinitionChangeRows.Identity(second));
    }
}
