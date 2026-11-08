// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_EventStoreAccessEvents.given;

public class kernel_entitlement_schemas : Specification
{
    protected static readonly PrincipalKey _holder = new(PrincipalKey.LocalIssuer, "app", true);
    protected EventTypes.EventTypes _eventTypes;
    protected JsonSerializerOptions _options;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(EventStoreAccessGranted), typeof(EventStoreAccessRevoked)]);
        _eventTypes = new(types, Substitute.For<IStorage>(), NullLogger<EventTypes.EventTypes>.Instance);

        // Mirrors the kernel event serializer: camel case, concept converters and null properties omitted.
        _options = new JsonSerializerOptions(Globals.JsonSerializerOptions) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    }

    protected JsonObject Serialize(object content) => (JsonObject)JsonSerializer.SerializeToNode(content, content.GetType(), _options)!;

    protected IList<JsonSchemaValidationError> Validate(object content, params string[] withoutProperties)
    {
        var json = Serialize(content);
        foreach (var property in withoutProperties) json.Remove(property);
        return _eventTypes.GetJsonSchema(content.GetType()).Validate(json);
    }
}
