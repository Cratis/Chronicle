// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumns;

public class when_resolving_composite_key_shapes : Specification
{
    int _object;
    int _reference;
    int _array;
    int _scalar;
    int _concept;

    void Because()
    {
        _object = Count("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}}}}""");
        _reference = Count("""{"type":"object","definitions":{"key":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}}},"properties":{"id":{"$ref":"#/definitions/key"}}}""");
        _array = Count("""{"type":"object","properties":{"id":{"type":"array","items":{"type":"string"}}}}""");
        _scalar = Count("""{"type":"object","properties":{"id":{"type":"string"}}}""");
        _concept = Count("""{"type":"object","properties":{"id":{"type":"object","format":"guid"}}}""");
    }

    [Fact] void should_install_for_an_object_key() => _object.ShouldEqual(1);
    [Fact] void should_install_for_a_referenced_object_key() => _reference.ShouldEqual(1);
    [Fact] void should_not_install_for_an_array_key() => _array.ShouldEqual(0);
    [Fact] void should_not_install_for_a_scalar_key() => _scalar.ShouldEqual(0);
    [Fact] void should_not_install_for_a_primitive_concept_key() => _concept.ShouldEqual(0);

    static int Count(string schema)
    {
        using var context = new ReadModelDbContext(new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite("Data Source=:memory:").Options, "shapes", ProjectedColumns.ForSchema(JsonSchema.FromJson(schema)), Substitute.For<IReadModelMigrator>());
        return ShadowColumns.For(context).Count;
    }
}
