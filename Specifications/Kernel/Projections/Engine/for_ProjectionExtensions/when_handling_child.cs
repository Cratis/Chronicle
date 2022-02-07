// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.for_ProjectionExtensions.when_applying_from_filter
{
    public class when_handling_child : given.an_observable_and_event_setup
    {
        const string key = "42";
        PropertyPath children_property = "Children";
        PropertyPath identified_by_property = "Id";
        ValueProvider<AppendedEvent> key_resolver = (_) => key;
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> property_mappers = Array.Empty<PropertyMapper<AppendedEvent, ExpandoObject>>();

        void Establish() => observable.Child(children_property, identified_by_property, key_resolver, property_mappers);

        void Because() => observable.OnNext(event_context);

        [Fact] void should_add_child_to_changeset() => changeset.Verify(_ => _.AddChild(children_property, identified_by_property, key, property_mappers, ArrayIndexer.NoIndexers), Once());
    }
}
