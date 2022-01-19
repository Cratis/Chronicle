// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.for_ProjectionExtensions.when_applying_from_filter
{
    public class when_projecting_to_root_property : given.an_observable_and_event_setup
    {
        IEnumerable<PropertyMapper<Event, ExpandoObject>> property_mappers = Array.Empty<PropertyMapper<Event, ExpandoObject>>();

        void Establish() => observable.Project("", "Id", _ => 42, property_mappers);

        void Because() => observable.OnNext(event_context);

        [Fact] void should_set_properties_on_changeset() => changeset.Verify(_ => _.SetProperties(property_mappers), Once());
    }
}
