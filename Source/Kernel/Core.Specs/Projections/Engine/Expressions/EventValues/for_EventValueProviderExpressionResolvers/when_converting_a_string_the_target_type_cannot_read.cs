// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_converting_a_string_the_target_type_cannot_read : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = "acme-renamed";
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.String, Format = "object-id" }, "Payload")(@event);

    [Fact] void should_leave_the_value_unconverted() => _result.ShouldEqual("acme-renamed");
}
