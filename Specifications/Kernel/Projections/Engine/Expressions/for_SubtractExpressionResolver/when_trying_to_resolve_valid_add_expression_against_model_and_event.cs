// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions.for_SubtractExpressionResolver;

public class when_trying_to_resolve_valid_add_expression_against_model_and_event : Specification
{
    SubtractExpressionResolver resolver;
    AppendedEvent @event;
    ExpandoObject target;

    void Establish()
    {
        target = new();
        dynamic targetAsDynamic = target;
        targetAsDynamic.targetProperty = 42d;
        var content = new JsonObject
        {
            ["sourceProperty"] = 2d
        };
        @event = new(new(0, new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)), new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", DateTimeOffset.UtcNow), content);
        resolver = new();
    }

    void Because() => resolver.Resolve("targetProperty", "$subtract(sourceProperty)")(@event, target, ArrayIndexers.NoIndexers);

    [Fact] void should_resolve_to_a_propertymapper_that_can_subtract_from_the_property() => ((double)((dynamic)target).targetProperty).ShouldEqual(40d);
}
