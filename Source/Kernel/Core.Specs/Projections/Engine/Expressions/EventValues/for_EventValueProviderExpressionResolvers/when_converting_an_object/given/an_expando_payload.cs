// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Engine.Expressions.given;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers.when_converting_an_object.given;

public class an_expando_payload : an_appended_event
{
    protected ExpandoObject _input;
    protected IDictionary<string, object?> _members;
    protected object _objectResult;
    protected object _unspecifiedResult;
    EventValueProviderExpressionResolvers _resolvers;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _input = new();
        _members = _input;
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    protected object Resolve(JsonObjectType type, string? format = null) =>
        _resolvers.Resolve(new JsonSchemaProperty { Type = type, Format = format }, "Payload")(@event);
}
