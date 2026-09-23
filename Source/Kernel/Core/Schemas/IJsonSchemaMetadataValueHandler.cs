// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Defines a system that can handle a property for a specific metadata type within a specific
/// <see cref="SchemaMetadataCategory"/>.
/// </summary>
/// <remarks>
/// <see cref="Category"/> is what keeps a compliance handler (e.g. for <c language="csharp">PII</c>) and a security
/// handler (e.g. for <c language="csharp">EncryptedSubject</c>) from ever being able to answer for the other's schema
/// key, even though both are discovered into the same <see cref="Cratis.Types.IInstancesOf{T}"/> pool and dispatched
/// by the same <see cref="IJsonSchemaMetadataManager"/>. <see cref="Type"/> only has to be unique within a
/// <see cref="Category"/>, not globally.
/// </remarks>
public interface IJsonSchemaMetadataValueHandler
{
    /// <summary>
    /// Gets the <see cref="SchemaMetadataCategory"/> this handler belongs to.
    /// </summary>
    SchemaMetadataCategory Category { get; }

    /// <summary>
    /// Gets the metadata type it supports, within <see cref="Category"/>.
    /// </summary>
    SchemaMetadataTypeName Type { get; }

    /// <summary>
    /// Apply to the given value.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the value belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the value belongs to.</param>
    /// <param name="identifier">Identifier to use.</param>
    /// <param name="value">Value to apply to.</param>
    /// <returns>Applied value.</returns>
    Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value);

    /// <summary>
    /// Release a given value.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the value belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the value belongs to.</param>
    /// <param name="identifier">Identifier to use.</param>
    /// <param name="value">Value to release.</param>
    /// <returns>Released value.</returns>
    Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value);
}
