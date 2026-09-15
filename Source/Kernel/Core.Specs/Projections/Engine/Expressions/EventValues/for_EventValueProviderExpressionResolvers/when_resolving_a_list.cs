// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_resolving_a_list : given.an_appended_event
{
    EventValueProviderExpressionResolvers _resolvers;
    List<int> _input;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _input = [42, 7, 42];
        ((IDictionary<string, object?>)@event.Content)["Payload"] = _input;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.Array, Item = new JsonSchema { Type = JsonObjectType.Integer } }, "Payload")(@event);

    [Fact] void should_convert_the_list_elements() => _result.ShouldBeOfExactType<List<object>>();
    [Fact] void should_preserve_the_elements_in_order() => ((List<object>)_result).SequenceEqual([42, 7, 42]).ShouldBeTrue();
    [Fact] void should_not_change_the_input() => _input.SequenceEqual([42, 7, 42]).ShouldBeTrue();
}
