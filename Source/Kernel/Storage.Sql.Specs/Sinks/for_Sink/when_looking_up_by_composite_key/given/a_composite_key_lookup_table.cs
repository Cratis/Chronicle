// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_looking_up_by_composite_key.given;

public class a_composite_key_lookup_table : when_writing_a_composite_key.given.a_composite_key_sink
{
    protected CompositeKeyLookupResult _result;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();

    protected Task<CompositeKeyLookupResult> Lookup(string first, string second)
    {
        IDictionary<string, object?> parts = new ExpandoObject();
        parts["first"] = first;
        parts["second"] = second;
        return ((ISinkCompositeKeyLookup)_sink).FindByCompositeKey(new Key((ExpandoObject)parts, ArrayIndexers.NoIndexers));
    }

    protected async Task SetStamp(string? stamp)
    {
        await using var context = CreateContext();
        var sql = context.GetService<ISqlGenerationHelper>();
        var command = $"UPDATE {sql.DelimitIdentifier(Table)} SET {sql.DelimitIdentifier(SqlCompositeKeyShadow.ColumnName)}={{0}}";
        await context.Database.ExecuteSqlInterpolatedAsync(FormattableStringFactory.Create(command, stamp));
    }

    protected async Task<CompositeKeyLookupResult[]> LookupLegacyStampedAndCollidingRows()
    {
        await Write("a", "b", "legacy");
        await Write("x_y", "z", "ambiguous");
        await SetStamp(null);
        await Write("stamped", "row", "stamped");
        await Write("c_d", "e", "owner");
        await Write("c", "d_e", "merged");
        return [await Lookup("a", "b"), await Lookup("x_y", "z"), await Lookup("stamped", "row"), await Lookup("c", "d_e")];
    }
}
