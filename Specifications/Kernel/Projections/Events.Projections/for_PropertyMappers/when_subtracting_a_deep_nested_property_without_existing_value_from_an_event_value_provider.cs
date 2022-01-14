// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Properties;

namespace Cratis.Events.Projections.for_PropertyMappers
{
    public class when_subtracting_a_deep_nested_property_without_existing_value_from_an_event_value_provider : Specification
    {
        PropertyMapper<Event, ExpandoObject> property_mapper;
        Event @event;
        ExpandoObject result;
        Event provided_event;

        void Establish()
        {
            dynamic content = new ExpandoObject();
            result = new();
            @event = new Event(0, new  EventType("02405794-91e7-4e4f-8ad1-f043070ca297", 1), DateTimeOffset.UtcNow, "2f005aaf-2f4e-4a47-92ea-63687ef74bd4", content);

            property_mapper = PropertyMappers.SubtractWithEventValueProvider("deep.nested.property", _ =>
            {
                provided_event = _;
                return 2d;
            });
        }

        void Because() => property_mapper(@event, result);

        [Fact] void should_result_in_expected_value() => ((object)((dynamic)result).deep.nested.property).ShouldEqual(-2d);
        [Fact] void should_pass_the_event_to_the_value_provider() => provided_event.ShouldEqual(@event);
    }
}
