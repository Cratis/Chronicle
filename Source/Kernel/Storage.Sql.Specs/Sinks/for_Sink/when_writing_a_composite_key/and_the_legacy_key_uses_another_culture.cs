// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_the_legacy_key_uses_another_culture : given.a_composite_key_sink
{
    string _id;
    string _stamp;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();
    protected override JsonSchema CreateSchema() => JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"number","format":"decimal"},"second":{"type":"string"}}},"name":{"type":"string"}}}""");

    async Task Because()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var parts = new ExpandoObject();
            ((IDictionary<string, object?>)parts)["first"] = 1.5m;
            ((IDictionary<string, object?>)parts)["second"] = "b";
            await Write(parts, "French");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
        await using var context = CreateContext();
        _id = await context.Database.SqlQuery<string>($"SELECT id AS Value FROM composite_shadows").SingleAsync();
        _stamp = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows").SingleAsync();
    }

    [Fact] void should_preserve_the_culture_sensitive_legacy_primary_key() => _id.ShouldEqual("1,5_b");
    [Fact] void should_stamp_an_invariant_canonical_encoding() => _stamp.ShouldEqual("~k1:3:1.51:b");
}
