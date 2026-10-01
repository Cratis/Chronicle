// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_updating_for_an_erased_subject.given;

/// <summary>
/// The whole #4453 cycle through the real compliance stack and converters: a read model is stored with its
/// subject's personal data encrypted, the subject is erased exactly the way PIIManager erases one (fence first,
/// then the key). <see cref="UpdateTwice"/> then updates the read model the way the projection pipeline does -
/// released, changed in a member that holds no personal data, and applied again - twice, to show the stored shape
/// is stable.
/// </summary>
public abstract class a_read_model_for_an_erased_subject : Specification
{
    protected const string EventStore = "test-store";
    protected const string Namespace = "test-namespace";
    protected const string Subject = "erased-subject";
    protected const string UpdatedStatus = "updated";

    protected InMemoryEncryptionKeyStorage _keyStorage;
    protected ReadModelsCompliance _compliance;
    protected JsonSchema _schema;
    protected ExpandoObject _stored;
    protected ExpandoObject _releasedAfterErasure;
    protected ExpandoObject _storedAfterUpdate;
    protected ExpandoObject _releasedAfterUpdate;
    protected ExpandoObject _storedAfterSecondUpdate;
    protected ExpandoObject _releasedAfterSecondUpdate;
    protected Exception? _exception;

    protected abstract string SchemaJson { get; }

    protected abstract IEnumerable<string> PersonalValues { get; }

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(SchemaJson);
        _keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keyStorage, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));

        var state = CreateState();
        ((IDictionary<string, object?>)state)["status"] = "registered";
        _stored = await _compliance.Apply(EventStore, Namespace, _schema, Subject, state);

        await _keyStorage.RecordErasureFor(EventStore, Namespace, Subject);
        await _keyStorage.DeleteFor(EventStore, Namespace, Subject);
    }

    protected async Task UpdateTwice()
    {
        try
        {
            _releasedAfterErasure = await _compliance.Release(EventStore, Namespace, _schema, _stored);
            _storedAfterUpdate = await Update(_releasedAfterErasure);
            _releasedAfterUpdate = await _compliance.Release(EventStore, Namespace, _schema, _storedAfterUpdate);
            _storedAfterSecondUpdate = await Update(_releasedAfterUpdate);
            _releasedAfterSecondUpdate = await _compliance.Release(EventStore, Namespace, _schema, _storedAfterSecondUpdate);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    protected abstract ExpandoObject CreateState();

    protected static object? ValueOf(ExpandoObject instance, string property) => ((IDictionary<string, object?>)instance)[property];

    protected static string Serialized(object? value) => JsonSerializer.Serialize(value);

    protected bool StoresNoPersonalValue(ExpandoObject instance)
    {
        var serialized = Serialized(instance);
        return PersonalValues.All(_ => !serialized.Contains(_, StringComparison.Ordinal));
    }

    async Task<ExpandoObject> Update(ExpandoObject released)
    {
        // What the projection pipeline does on the next event: the released state is changed in a member that
        // holds no personal data and handed back for protection before it is stored.
        var copy = new ExpandoObject();
        foreach (var (key, value) in (IDictionary<string, object?>)released)
        {
            ((IDictionary<string, object?>)copy)[key] = value;
        }

        ((IDictionary<string, object?>)copy)["status"] = UpdatedStatus;
        ((IDictionary<string, object?>)copy)[WellKnownProperties.Subject] = Subject;
        return await _compliance.Apply(EventStore, Namespace, _schema, Subject, copy);
    }
}
