// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Serialization;

namespace Cratis.Chronicle.Discovery.for_DiscoveryReportBuilder.when_building;

public class and_a_projection_could_not_be_built : Chronicle.for_EventStore.when_registering_all.given.an_event_store_with_a_projection_that_cannot_be_built
{
    DiscoveryReportBuilder _builder;
    DiscoveryReport _report;

    void Establish()
    {
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.All.Returns([]);
        var reactors = (Reactors.Reactors)RuntimeHelpers.GetUninitializedObject(typeof(Reactors.Reactors));
        var handlers = typeof(Reactors.Reactors).GetField("_handlers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        handlers.SetValue(reactors, Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(handlers.FieldType.GetGenericArguments())));
        _builder = new(eventTypes, new Constraints(_eventStore, []), reactors, _eventStore.Reducers, (Projections.Projections)_projections, new DefaultNamingPolicy());
    }

    void Because() => _report = _builder.Build();

    [Fact] void should_report_only_the_buildable_projection() => _report.CountOf(ArtifactKind.Projection).ShouldEqual(1);
    [Fact] void should_not_report_a_read_model_without_a_handler() => _report.CountOf(ArtifactKind.ReadModel).ShouldEqual(1);
}
