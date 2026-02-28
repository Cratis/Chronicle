// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_Projections.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IEventTypes _eventTypes;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IClientArtifactsActivator _artifactsActivator;
    protected IEventSerializer _eventSerializer;
    protected IServiceProvider _serviceProvider;
    protected JsonSerializerOptions _jsonSerializerOptions;
    protected INamingPolicy _namingPolicy;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventTypes = Substitute.For<IEventTypes>();
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _artifactsActivator = Substitute.For<IClientArtifactsActivator>();
        _eventSerializer = Substitute.For<IEventSerializer>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _jsonSerializerOptions = new();
        _namingPolicy = new DefaultNamingPolicy();
    }
}
