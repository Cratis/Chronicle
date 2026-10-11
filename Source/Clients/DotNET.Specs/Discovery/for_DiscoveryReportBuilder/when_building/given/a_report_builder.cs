// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Reducers;
using Cratis.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Discovery.for_DiscoveryReportBuilder.when_building.given;

public class a_report_builder : Reactors.for_Reactors.given.all_dependencies
{
    protected Constraints _constraints;
    protected IReducers _reducers;
    protected Projections.Projections _projections;
    private protected DiscoveryReportBuilder _builder;
    protected DiscoveryReport _report;
    protected EventType _eventType;

    void Establish()
    {
        _eventType = new("Discovered", EventTypeGeneration.First);
        _eventTypes.All.Returns(ImmutableList.Create(_eventType));
        _eventTypes.GetClrTypeFor(_eventType.Id).Returns(typeof(Discovered));
        _clientArtifactsProvider.Projections.Returns([]);
        _clientArtifactsProvider.ModelBoundProjections.Returns([]);
        _constraints = new(_eventStore, []);
        _reducers = Substitute.For<IReducers>();
        _reducers.GetAllHandlers().Returns([]);
        _projections = new(_eventStore, _eventTypes, _clientArtifactsProvider, new DefaultNamingPolicy(), _artifactActivator, new JsonSerializerOptions(), NullLogger<Projections.Projections>.Instance);
        _builder = new(_eventTypes, _constraints, _reactors, _reducers, _projections, new DefaultNamingPolicy());
    }

    public record Discovered(string Name);
    public record Model(string Name);
}
