// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Rules;
using Cratis.Chronicle.Schemas;
using Cratis.Models;

namespace Cratis.Chronicle.Projections.for_Projections.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IProjectionWatcherManager _projectionWatcherManager;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IRulesProjections _rulesProjections;
    protected IJsonSchemaGenerator _schemaGenerator;
    protected IModelNameResolver _modelNameResolver;
    protected IEventSerializer _eventSerializer;
    protected IServiceProvider _serviceProvider;
    protected JsonSerializerOptions _jsonSerializerOptions;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventTypes = Substitute.For<IEventTypes>();
        _projectionWatcherManager = Substitute.For<IProjectionWatcherManager>();
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _rulesProjections = Substitute.For<IRulesProjections>();
        _schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        _modelNameResolver = Substitute.For<IModelNameResolver>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _jsonSerializerOptions = new();
    }
}
