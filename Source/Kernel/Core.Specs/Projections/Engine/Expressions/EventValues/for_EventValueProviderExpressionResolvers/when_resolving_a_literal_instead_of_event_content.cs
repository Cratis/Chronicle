// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_a_literal_instead_of_event_content : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance);
        ((IDictionary<string, object?>)@event.Content)["True"] = false;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Boolean }, "True")(@event);

    [Fact] void should_choose_the_literal_over_the_event_property() => _result.ShouldEqual(true);
}
