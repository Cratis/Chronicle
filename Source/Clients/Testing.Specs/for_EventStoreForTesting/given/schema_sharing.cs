// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.given;

[Collection(nameof(schema_sharing_collection))]
public class schema_sharing : store_identity
{
    protected EventStoreForTesting _other;
    protected JsonSchema _schema;
    protected JsonSchema _otherSchema;
    bool _previousSharing;

    protected static JsonSchemaGenerator GeneratorFor(EventStoreForTesting store) => ((IsolatedJsonSchemaGenerator)store.JsonSchemaGenerator).SharedGenerator;

    protected static JsonSchema SchemaFor(EventStoreForTesting store) => store.EventTypes.GetSchemaFor(store.EventTypes.GetEventTypeFor(typeof(StoreIdentityRecorded)).Id);

    void Establish()
    {
        _previousSharing = EventStoreForTesting.ShareSchemaGenerator;
        EventStoreForTesting.ShareSchemaGenerator = true;
        _store = new(null, _artifacts);
    }

    void Destroy()
    {
        EventStoreForTesting.ShareSchemaGenerator = _previousSharing;
        _other?.Dispose();
    }
}
