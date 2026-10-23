// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Keeps mutable schemas local to a test store while reusing structural generation work.
/// </summary>
/// <param name="generator">The shared underlying schema generator.</param>
internal sealed class IsolatedJsonSchemaGenerator(JsonSchemaGenerator generator) : IJsonSchemaGenerator
{
    /// <summary>
    /// Gets the underlying schema generator shared by equivalent stores.
    /// </summary>
    internal JsonSchemaGenerator SharedGenerator => generator;

    /// <inheritdoc/>
    public JsonSchema Generate(Type type) => Clone(generator.Generate(type));

    /// <inheritdoc/>
    public JsonSchema Generate(Type type, IEventTypes eventTypes) => Clone(generator.Generate(type, eventTypes));

    /// <inheritdoc/>
    public JsonSchema GenerateForReadModel(Type type) => Clone(generator.GenerateForReadModel(type));

    /// <inheritdoc/>
    public JsonSchema GenerateLegacyEventType(Type type) => Clone(generator.GenerateLegacyEventType(type));

    /// <inheritdoc/>
    public JsonSchema GenerateLegacyEventType(Type type, IEventTypes eventTypes) => Clone(generator.GenerateLegacyEventType(type, eventTypes));

    static JsonSchema Clone(JsonSchema schema) => new(schema.Node.DeepClone().AsObject());
}
