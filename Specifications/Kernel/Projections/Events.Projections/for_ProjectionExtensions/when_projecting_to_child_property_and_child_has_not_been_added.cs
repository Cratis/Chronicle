// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Properties;

namespace Cratis.Events.Projections.for_ProjectionExtensions.when_applying_from_filter
{
    public class when_projecting_to_child_property_and_child_has_not_been_added : given.an_observable_and_event_setup
    {
        const string key = "42";
        PropertyPath children_property = "Children";
        PropertyPath identified_by_property = "Id";
        ValueProvider<Event> key_resolver = (_) => key;
        IEnumerable<PropertyMapper<Event, ExpandoObject>> property_mappers = Array.Empty<PropertyMapper<Event, ExpandoObject>>();

        ExpandoObject child;

        void Establish()
        {
            child = new();
            changeset.Setup(_ => _.HasChildBeenAddedWithKey(children_property, key)).Returns(false);
            changeset.Setup(_ => _.GetChildByKey<ExpandoObject>(children_property, identified_by_property, key)).Returns(child);
            observable.Project(children_property, identified_by_property, key_resolver, property_mappers);
        }

        void Because() => observable.OnNext(event_context);

        [Fact] void should_set_child_properties_on_changeset() => changeset.Verify(_ => _.SetChildProperties(child, children_property, identified_by_property, key_resolver, property_mappers), Once());
    }
}
