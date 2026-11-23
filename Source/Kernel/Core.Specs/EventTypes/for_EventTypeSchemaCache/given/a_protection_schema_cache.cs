// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeSchemaCache.given;

public class a_protection_schema_cache : a_schema_cache
{
    protected JsonSchema _own;
    protected JsonSchema _higher;
    protected EventTypeDefinition _definition;

    void Establish()
    {
        _own = JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string"}}}""");
        _higher = JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _definition = new(_eventTypeId, EventTypeOwner.Client, false, [new(_firstGeneration, _own), new(_secondGeneration, _higher)], []);
        _eventTypesStorage.GetDefinition(_eventTypeId).Returns(_ => _definition);
    }

    protected ValueTask<JsonSchema> Resolve() => _cache.GetProtectionSchemaFor(_eventStore, _eventTypeId, _firstGeneration, _own);
    protected int DefinitionLookups() => _eventTypesStorage.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventTypesStorage.GetDefinition));
}
