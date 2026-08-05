// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.given;

public class a_model_bound_projections : Specification
{
    internal ModelBoundProjections projections;
    protected IClientArtifactsProvider _clientArtifactsProvider;
    protected INamingPolicy _namingPolicy;
    protected IEventTypes _eventTypes;
    protected ILogger<Projections> _logger;

    void Establish()
    {
        _clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        _namingPolicy = Substitute.For<INamingPolicy>();
        _eventTypes = new EventTypesForSpecifications([typeof(TestEvent)]);
        _logger = Substitute.For<ILogger<Projections>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        projections = new ModelBoundProjections(
            _clientArtifactsProvider,
            _namingPolicy,
            _eventTypes,
            _logger);
    }
}
