// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_opting_out : given.schema_sharing
{
    void Because()
    {
        EventStoreForTesting.ShareSchemaGenerator = false;
        _other = new(null, _artifacts);
        _schema = SchemaFor(_store);
        _otherSchema = SchemaFor(_other);
    }

    [Fact] void should_not_share_the_underlying_generator() => ReferenceEquals(GeneratorFor(_store), GeneratorFor(_other)).ShouldBeFalse();
    [Fact] void should_preserve_generated_schemas() => _schema.ToJson().ShouldEqual(_otherSchema.ToJson());
}
