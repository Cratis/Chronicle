// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

/// <summary>
/// A string-typed property can still target a non-string type through its format, and the conversion has to
/// actually happen: a key left as raw text does not match one that converted, so a join silently stops pairing
/// and produces a duplicate child instead of failing.
/// </summary>
public class when_resolving_a_string_property_whose_format_targets_another_type : given.an_appended_event
{
    const string Value = "9b0e6a7e-0f2f-4f9a-9a3f-2b6d1f7c5e41";

    EventValueProviderExpressionResolvers _resolvers;
    object _result;

    void Establish()
    {
        _resolvers = new(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        ((IDictionary<string, object?>)@event.Content)["Payload"] = Value;
    }

    void Because() => _result = _resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.String, Format = "guid" }, "Payload")(@event);

    [Fact] void should_convert_to_the_formatted_type() => _result.ShouldBeOfExactType<Guid>();
    [Fact] void should_convert_the_value() => _result.ShouldEqual(Guid.Parse(Value));
}
