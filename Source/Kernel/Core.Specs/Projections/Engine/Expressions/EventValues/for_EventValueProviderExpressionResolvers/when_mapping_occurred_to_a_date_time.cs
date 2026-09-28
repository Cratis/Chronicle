// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_EventValueProviderExpressionResolvers;

public class when_mapping_occurred_to_a_date_time : given.an_appended_event
{
    object _result;

    void Because()
    {
        var resolvers = new EventValueProviderExpressionResolvers(new TypeFormats(), Substitute.For<ILogger<EventValueProviderExpressionResolvers>>());
        _result = resolvers.Resolve(new JsonSchemaProperty { Type = JsonObjectType.String, Format = "date-time" }, "$eventContext(Occurred)")(@event);
    }

    [Fact] void should_preserve_the_occurred_instant() => _result.ShouldEqual(occurred.UtcDateTime);
}
