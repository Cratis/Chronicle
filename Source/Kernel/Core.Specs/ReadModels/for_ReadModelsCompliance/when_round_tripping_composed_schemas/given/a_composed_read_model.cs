// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas.given;

public abstract class a_composed_read_model : Specification
{
    protected const string Subject = "composed-subject";
    protected const string PersonalValue = "personal-plaintext";
    protected const string Pii = """{ "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] }""";
    protected const string PublicProperties = """
        "status": { "type": "string" }, "optionalStatus": { "type": ["string", "null"] }
        """;

    protected InMemoryEncryptionKeyStorage _keys;
    protected ReadModelsCompliance _compliance;
    protected JsonSchema _schema;
    protected ExpandoObject _state;
    protected ExpandoObject _stored;
    protected ExpandoObject _released;
    protected ExpandoObject _releasedAfterErasure;
    protected ExpandoObject _storedAfterErasure;
    protected ExpandoObject _releasedAfterUpdate;
    protected JsonObject _releasedJsonAfterUpdate;
    protected Exception? _jsonReleaseError;

    protected abstract string SchemaJson { get; }

    void Establish()
    {
        _schema = JsonSchema.FromJson(SchemaJson);
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        _state = State();
    }

    protected virtual ExpandoObject State() => CreateState(("localSecret", PersonalValue), ("baseSecret", PersonalValue), ("status", "updated"), ("optionalStatus", null));

    protected async Task RoundTrip()
    {
        _stored = await _compliance.Apply("store", "Default", _schema, Subject, _state);
        _released = await _compliance.Release("store", "Default", _schema, _stored);
        await _keys.RecordErasureFor("store", "Default", Subject);
        await _keys.DeleteFor("store", "Default", Subject);
        _releasedAfterErasure = await _compliance.Release("store", "Default", _schema, _stored);
        _storedAfterErasure = await _compliance.Apply("store", "Default", _schema, Subject, _state);
        _releasedAfterUpdate = await _compliance.Release("store", "Default", _schema, _storedAfterErasure);
        _jsonReleaseError = await Catch.Exception(async () => _releasedJsonAfterUpdate = await _compliance.ReleaseJson("store", "Default", _schema, JsonSerializer.SerializeToNode(_storedAfterErasure)!.AsObject()));
    }

    protected static ExpandoObject CreateState(params (string Name, object? Value)[] properties)
    {
        var result = new ExpandoObject();
        foreach (var (name, value) in properties)
        {
            ((IDictionary<string, object?>)result)[name] = value;
        }
        return result;
    }

    protected static object? Value(ExpandoObject instance, string name) => ((IDictionary<string, object?>)instance).TryGetValue(name, out var value) ? value : null;

    protected static bool IsEncrypted(object? value) => value is string text && ProtectedValueCodec.TryDecodeCipherText(new Encryption(), text, out _);

    protected static bool ContainsPersonalValue(object instance) => JsonSerializer.Serialize(instance).Contains(PersonalValue, StringComparison.Ordinal);
}
