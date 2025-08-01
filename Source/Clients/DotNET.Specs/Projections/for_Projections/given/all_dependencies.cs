// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Serialization;

namespace Cratis.Chronicle.Projections.for_Projections.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IProjectionWatcherManager _projectionWatcherManager;
    protected IClientArtifactsProvider _clientArtifacts;
    internal IRulesProjections _rulesProjections;
    protected IEventSerializer _eventSerializer;
    protected IServiceProvider _serviceProvider;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected INamingPolicy _namingPolicy;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventTypes = Substitute.For<IEventTypes>();
        _projectionWatcherManager = Substitute.For<IProjectionWatcherManager>();
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _rulesProjections = Substitute.For<IRulesProjections>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _jsonSerializerOptions = new();
        _namingPolicy = Substitute.For<INamingPolicy>();
    }
}
