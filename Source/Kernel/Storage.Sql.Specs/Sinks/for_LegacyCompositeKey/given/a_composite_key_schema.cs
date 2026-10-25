// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_LegacyCompositeKey.given;

public class a_composite_key_schema : Specification
{
    protected const string FirstPart = "6f1c2c43-6c8c-4b3c-9d1c-3a7b4f0e2a11";

    private protected LegacyCompositeKey _key;

    void Establish() => _key = CreateFor("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string","format":"guid"},"second":{"type":"integer","format":"uint64"}}}}}""")!;

    private protected static LegacyCompositeKey? CreateFor(string schemaJson)
    {
        var schema = JsonSchema.FromJson(schemaJson);
        return LegacyCompositeKey.For(schema, ProjectedColumns.ForSchema(schema).First(column => column.IsKey));
    }
}
