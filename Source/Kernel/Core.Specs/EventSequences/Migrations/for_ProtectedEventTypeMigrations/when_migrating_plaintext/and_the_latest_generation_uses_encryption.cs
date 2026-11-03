// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_the_latest_generation_uses_encryption : given.a_token_protected_only_in_the_latest_generation
{
    JsonObject _released;

    async Task Establish()
    {
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new EncryptedSubjectValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schemas[2] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"token":{"type":"string","security":[{"metadataType":"EncryptedSubject","details":""}]}}}""");
        _definition = _definition with { Generations = [new(1, _schemas[0]), new(2, _schemas[1]), new(3, _schemas[2])] };
        _protectedContent = await _manager.Apply(Store, Namespace, _schemas[2], Subject, _plaintext);
        _protectedSource = _converter.ToExpandoObject(_protectedContent, _schemas[2]);
        _sut = new(_eventTypes, _migrations, _manager, _converter);
    }

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        await _keys.DeleteFor(Store, Namespace, Subject);
        var schema = GenerationProtectionSchemas.For(_definition, 1);
        _released = await _manager.Release(Store, Namespace, schema, Subject, _converter.ToJsonObject(_migrated[(Concepts.Events.EventTypeGeneration)1], schema));
    }

    [Fact] void should_protect_the_first_generation() => StoredGeneration(1).ShouldNotContain(Token);
    [Fact] void should_protect_the_second_generation() => StoredGeneration(2).ShouldNotContain(Token);
    [Fact] void should_keep_encryption_independent_of_pii_erasure() => _released["token"].GetValue<string>().ShouldEqual(Token);
}
