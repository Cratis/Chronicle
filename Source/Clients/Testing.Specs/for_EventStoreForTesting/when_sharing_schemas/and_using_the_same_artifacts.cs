// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_using_the_same_artifacts : given.schema_sharing
{
    void Because()
    {
        _other = new(null, _artifacts);
        _schema = SchemaFor(_store);
        _otherSchema = SchemaFor(_other);
    }

    [Fact] void should_share_the_underlying_generator() => GeneratorFor(_store).ShouldBeSame(GeneratorFor(_other));
    [Fact] void should_keep_each_stores_schema_separate() => ReferenceEquals(_schema, _otherSchema).ShouldBeFalse();
    [Fact] void should_keep_each_stores_generator_wrapper_separate() => ReferenceEquals(_store.JsonSchemaGenerator, _other.JsonSchemaGenerator).ShouldBeFalse();
    [Fact] void should_expose_identical_schemas() => _schema.ToJson().ShouldEqual(_otherSchema.ToJson());
}
