// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IArtifactActivator"/>.
/// </summary>
/// <param name="rootServiceProvider">The root <see cref="IServiceProvider"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[IgnoreConvention]
public class ArtifactActivator(IServiceProvider rootServiceProvider, ILoggerFactory loggerFactory) : IArtifactActivator
{
    readonly ILogger _logger = loggerFactory.CreateLogger<ArtifactActivator>();

    /// <inheritdoc/>
    public ActivatedArtifact CreateInstance(Type artifactType) => CreateInstance(rootServiceProvider, artifactType);

    /// <inheritdoc/>
    public ActivatedArtifact CreateInstance(IServiceProvider scopedServiceProvider, Type artifactType)
    {
        _logger.ActivatingArtifact(artifactType);
        try
        {
            var instance = ActivatorUtilities.GetServiceOrCreateInstance(scopedServiceProvider, artifactType);
            return new ActivatedArtifact(instance, artifactType, loggerFactory);
        }
        catch (Exception ex)
        {
            _logger.ArtifactActivationFailed(artifactType, ex);
            throw new ArtifactActivationFailed(artifactType, ex);
        }
    }
}
