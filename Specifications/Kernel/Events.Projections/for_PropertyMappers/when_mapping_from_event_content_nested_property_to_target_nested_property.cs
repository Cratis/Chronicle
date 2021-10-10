// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections.for_PropertyMappers
{
    public class when_mapping_from_event_content_nested_property_to_target_nested_property : Specification
    {
        PropertyMapper property_mapper;
        Event @event;
        ExpandoObject result;

        void Establish()
        {
            result = new();
            dynamic content = new ExpandoObject();
            content.Nested = new ExpandoObject();
            content.Nested.SourceString = "Forty two";
            @event = new Event(0, "02405794-91e7-4e4f-8ad1-f043070ca297", DateTimeOffset.UtcNow, "2f005aaf-2f4e-4a47-92ea-63687ef74bd4", content);
            property_mapper = PropertyMappers.FromEventContent("Nested.SourceString", "TargetNested.TargetString");
        }

        void Because() => property_mapper(@event, result);

        [Fact] void should_copy_content_of_property_from_source_to_target() => ((string)((dynamic)result).TargetNested.TargetString).ShouldEqual("Forty two");
    }
}
