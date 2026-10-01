// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Defines a Json serializer that is schema-metadata aware - applying and releasing whatever a schema's
/// registered <see cref="IJsonSchemaMetadataValueHandler"/> instances say a value needs, regardless of which
/// <see cref="SchemaMetadataCategory"/> that value belongs to.
/// </summary>
/// <remarks>
/// This is the single, generalized engine both compliance (<c language="csharp">[PII]</c>) and security
/// (<c language="csharp">[Encrypted]</c>) values go through. Neither category is special-cased here - each
/// registered handler reports its own <see cref="SchemaMetadataCategory"/>, and this manager reads the matching
/// category's schema key for it. See <see cref="SchemaMetadataCategory"/> for why the categories are kept apart
/// at all.
/// </remarks>
public interface IJsonSchemaMetadataManager
{
    /// <summary>
    /// Apply schema metadata rules to JSON.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the value belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the value belongs to.</param>
    /// <param name="schema"><see cref="JsonSchema"/> that represents the object.</param>
    /// <param name="identifier">Identifier of the object.</param>
    /// <param name="json">JSON to apply rules for.</param>
    /// <returns>JSON with schema metadata rules applied.</returns>
    Task<JsonObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json);

    /// <summary>
    /// Apply schema metadata rules to the JSON of a read model.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the value belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the value belongs to.</param>
    /// <param name="schema"><see cref="JsonSchema"/> that represents the read model.</param>
    /// <param name="identifier">Identifier of the subject the values belong to.</param>
    /// <param name="json">JSON to apply rules for.</param>
    /// <returns>JSON with schema metadata rules applied.</returns>
    /// <remarks>
    /// Works like <see cref="Apply"/> with one difference: when the subject's key has been erased, a protected scalar
    /// is stored as its declared type's erased placeholder (null for nullable values). Whole protected containers
    /// use an empty storage marker and release as empty containers. A read model is derived state, and refusing would stop every later update of it while
    /// protecting nothing. Events go through <see cref="Apply"/>, where an erased subject still refuses the append.
    /// </remarks>
    Task<JsonObject> ApplyToReadModel(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json);

    /// <summary>
    /// Release JSON from schema metadata rules.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the value belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the value belongs to.</param>
    /// <param name="schema"><see cref="JsonSchema"/> that represents the object.</param>
    /// <param name="identifier">Identifier of the object.</param>
    /// <param name="json">JSON to release rules for.</param>
    /// <returns>Released version of the JSON.</returns>
    Task<JsonObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json);
}
