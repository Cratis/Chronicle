// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Properties;

namespace Cratis.Events.Projections
{
    public class when_applying_properties_that_does_not_cause_changes : Specification
    {
        Changeset<Event, ExpandoObject> changeset;
        ExpandoObject initial_state;
        Event @event;
        IEnumerable<PropertyMapper<Event, ExpandoObject>> property_mappers;
        Mock<IProjection> projection;

        void Establish()
        {
            initial_state = new();
            projection = new();

            ((dynamic)initial_state).Integer = 42;
            ((dynamic)initial_state).String = "Forty Two";
            dynamic nested = ((dynamic)initial_state).Nested = new ExpandoObject();
            nested.Integer = 43;
            nested.String = "Forty Three";

            property_mappers = new PropertyMapper<Event, ExpandoObject>[]
            {
                (_, target) => ((dynamic)target).Integer = 42,
                (_, target) => ((dynamic)target).String = "Forty Two",
                (_, target) => ((dynamic)target).Nested.Integer = 43,
                (_, target) => ((dynamic)target).Nested.String = "Forty Three",
            };

            @event = new Event(0, "some event", DateTimeOffset.UtcNow, string.Empty, new ExpandoObject());

            changeset = new(@event, initial_state);
        }

        void Because() => changeset.ApplyProperties(property_mappers);

        [Fact] void should_not_have_any_changes() => changeset.Changes.Count().ShouldEqual(0);
    }
}
