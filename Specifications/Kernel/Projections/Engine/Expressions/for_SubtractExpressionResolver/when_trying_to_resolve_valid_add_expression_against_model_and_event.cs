// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Events.Projections.Expressions.for_SubtractExpressionResolver
{
    public class when_trying_to_resolve_valid_add_expression_against_model_and_event : Specification
    {
        SubtractExpressionResolver resolver;
        Event @event;
        ExpandoObject target;

        void Establish()
        {
            target = new();
            dynamic targetAsDynamic = target;
            targetAsDynamic.targetProperty = 42d;
            var content = new ExpandoObject();
            dynamic contentAsDynamic = content;
            contentAsDynamic.sourceProperty = 2d;
            @event = new Event(0, new("f0b3b624-faa7-4358-ade4-24e89ad067ce", 1), DateTimeOffset.UtcNow, "a258b980-b9e4-4b99-b44b-7c72f8633af7", content);
            resolver = new();
        }

        void Because() => resolver.Resolve("targetProperty", "$subtract(sourceProperty)")(@event, target);

        [Fact] void should_resolve_to_a_propertymapper_that_can_subtract_from_the_property() => ((double)((dynamic)target).targetProperty).ShouldEqual(40d);
    }
}
