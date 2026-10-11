// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Discovery.for_DiscoveryReportBuilder.when_building;

public class and_every_kind_is_discovered : given.a_report_builder
{
    async Task Establish()
    {
        var constraint = Substitute.For<IConstraintDefinition>();
        constraint.Name.Returns(new ConstraintName("unique-name"));
        var provider = Substitute.For<ICanProvideConstraints>();
        provider.Provide().Returns([constraint]);
        _constraints = new(_eventStore, [provider]);
        await _constraints.Discover();
        await _reactors.Register("runtime", builder => builder.WithEventType(_eventType), (_, _) => Task.CompletedTask);
        var reducer = Substitute.For<IReducerHandler>();
        reducer.Id.Returns(new ReducerId("reduce"));
        reducer.ReducerType.Returns(typeof(string));
        reducer.ReadModelType.Returns(typeof(Model));
        _reducers.GetAllHandlers().Returns([reducer]);
        await _projections.Register<Model>(_ => { }, "project");
        _builder = new(_eventTypes, _constraints, _reactors, _reducers, _projections, new Cratis.Serialization.DefaultNamingPolicy());
    }

    void Because() => _report = _builder.Build();

    [Fact] void should_report_every_kind() => Enum.GetValues<ArtifactKind>().All(kind => _report.CountOf(kind) == 1).ShouldBeTrue();
    [Fact] void should_deduplicate_read_models() => _report.OfKind(ArtifactKind.ReadModel).Single().ClrType.ShouldEqual(typeof(Model));
    [Fact] void should_use_the_registered_id() => _report.OfKind(ArtifactKind.Projection).Single().Id.ShouldEqual("project");
    [Fact] void should_record_the_event_clr_type() => _report.OfKind(ArtifactKind.EventType).Single().ClrType.ShouldEqual(typeof(Discovered));
}
