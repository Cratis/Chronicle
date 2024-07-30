// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IEventSerializer"/> that does minimal to work with testing.
/// </summary>
public class NullEventSerializerForTesting : IEventSerializer
{
    /// <inheritdoc/>
    public Task<object> Deserialize(Type type, JsonObject json) => Task.FromResult((object)new { });

    /// <inheritdoc/>
    public Task<object> Deserialize(Type type, ExpandoObject expandoObject) => Task.FromResult((object)new { });

    /// <inheritdoc/>
    public Task<object> Deserialize(AppendedEvent @event) => Task.FromResult((object)new { });

    /// <inheritdoc/>
    public Task<JsonObject> Serialize(object @event) => Task.FromResult(new JsonObject());
}
