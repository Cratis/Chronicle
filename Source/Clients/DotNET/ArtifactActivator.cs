// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IArtifactActivator"/>.
/// </summary>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Singleton]
public class ArtifactActivator(ILoggerFactory loggerFactory) : IArtifactActivator
{
    readonly ILogger _logger = loggerFactory.CreateLogger<ArtifactActivator>();

    /// <inheritdoc/>
    public ActivatedArtifact CreateInstance(IServiceProvider serviceProvider, Type artifactType)
    {
        _logger.ActivatingArtifact(artifactType);
        try
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, artifactType);
            return new ActivatedArtifact(instance, artifactType, loggerFactory);
        }
        catch (Exception ex)
        {
            _logger.ArtifactActivationFailed(artifactType, ex);
            throw new ArtifactActivationFailed(artifactType, ex);
        }
    }
}
