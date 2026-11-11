// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_an_object_with_a_missing_property : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    IDictionary<string, object> _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        var payload = new ExpandoObject();
        ((IDictionary<string, object?>)payload)["first"] = "42";
        ((IDictionary<string, object?>)@event.Content)["Payload"] = payload;
    }

    void Because()
    {
        var schema = new JsonSchemaProperty { Type = JsonObjectType.Object };
        schema.Properties["first"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        schema.Properties["second"] = new JsonSchemaProperty { Type = JsonObjectType.Integer };
        _result = (IDictionary<string, object>)_resolvers.Resolve(schema, "Payload")(@event);
    }

    [Fact] void should_convert_the_present_property() => _result["first"].ShouldEqual(42);
    [Fact] void should_not_add_the_missing_property() => _result.ContainsKey("second").ShouldBeFalse();
}
