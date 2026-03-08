// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionExtensions.when_applying_from_filter;

public class when_projecting_to_root_property : given.an_observable_and_event_setup
{
    IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> _propertyMappers = [];

    void Establish() => _observable.Project(string.Empty, "Id", _propertyMappers);

    void Because() => _observable.OnNext(_eventContext);

    [Fact] void should_set_properties_on_changeset() => _changeset.Received(1).SetProperties(_propertyMappers, _eventContext.Key.ArrayIndexers);
}
