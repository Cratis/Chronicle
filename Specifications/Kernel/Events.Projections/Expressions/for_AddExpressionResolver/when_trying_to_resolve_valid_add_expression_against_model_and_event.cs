// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections.Expressions.for_AddExpressionResolver
{
    public class when_trying_to_resolve_valid_add_expression_against_model_and_event : Specification
    {
        AddExpressionResolver resolver;
        Event @event;
        ExpandoObject target;


        void Establish()
        {
            target = new();
            var content = new ExpandoObject();
            dynamic contentAsDynamic = content;
            contentAsDynamic.sourceProperty = 42d;
            @event = new Event(0, "f0b3b624-faa7-4358-ade4-24e89ad067ce", DateTimeOffset.UtcNow, "a258b980-b9e4-4b99-b44b-7c72f8633af7", content);
            resolver = new();
        }

        void Because() => resolver.Resolve("targetProperty", "$add(sourceProperty)")(@event, target);

        [Fact] void should_resolve_to_a_propertymapper_that_can_add_to_the_property() => ((double)((dynamic)target).targetProperty).ShouldEqual(42d);
    }
}
