// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_generating_through_each_entry_point : given.schema_sharing
{
    JsonSchema[] _schemas;
    JsonSchema[] _otherSchemas;
    string[] _originalSchemas;

    void Because()
    {
        _other = new(null, _artifacts);
        _schemas = GenerateSchemas(_store.JsonSchemaGenerator);
        _otherSchemas = GenerateSchemas(_other.JsonSchemaGenerator);
        _originalSchemas = _otherSchemas.Select(schema => schema.ToJson()).ToArray();
        foreach (var schema in _schemas)
        {
            schema.Title = "local change";
            schema.Properties["value"].Title = "local property change";
        }
    }

    [Fact] void should_clone_every_entry_point() => _schemas.Zip(_otherSchemas).All(pair => !ReferenceEquals(pair.First, pair.Second)).ShouldBeTrue();
    [Fact] void should_isolate_mutations_for_every_entry_point() => _otherSchemas.Select(schema => schema.ToJson()).ShouldEqual(_originalSchemas);
    [Fact] void should_leave_the_cached_schemas_unchanged() => GenerateSchemas(GeneratorFor(_store)).Select(schema => schema.ToJson()).ShouldEqual(_originalSchemas);

    JsonSchema[] GenerateSchemas(IJsonSchemaGenerator generator) =>
    [
        generator.Generate(typeof(StoreIdentityRecorded)),
        generator.Generate(typeof(StoreIdentityRecorded), _store.EventTypes),
        generator.GenerateForReadModel(typeof(StoreIdentityRecorded)),
        generator.GenerateLegacyEventType(typeof(StoreIdentityRecorded)),
        generator.GenerateLegacyEventType(typeof(StoreIdentityRecorded), _store.EventTypes)
    ];
}
