// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorMiddlewares"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorMiddlewares"/> class.
/// </remarks>
/// <param name="clientArtifacts"><see cref="IClientArtifactsProvider"/> to get the <see cref="IReactorMiddleware"/> types.</param>
/// <param name="clientArtifactsActivator"><see cref="IClientArtifactsActivator"/> for activating the middlewares.</param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/> for logging.</param>
[IgnoreConvention]
public class ReactorMiddlewaresActivator(
    IClientArtifactsProvider clientArtifacts,
    IClientArtifactsActivator clientArtifactsActivator,
    ILogger<ReactorMiddlewaresActivator> logger) : IActivateReactorMiddlewares
{
    /// <inheritdoc/>
    public IReactorMiddlewares Activate(IServiceProvider scopedServiceProvider)
    {
        var middlewares = new List<ActivatedArtifact<IReactorMiddleware>>();
        foreach (var middlewareType in clientArtifacts.ReactorMiddlewares)
        {
            var activationResult = clientArtifactsActivator.Activate<IReactorMiddleware>(scopedServiceProvider, middlewareType);
            if (activationResult.TryGetException(out var exception))
            {
                logger.FailedToActivateReactorMiddleware(middlewareType, exception);
                continue;
            }
            middlewares.Add(activationResult.AsT0);
        }
        return new ReactorMiddlewares(middlewares.ToArray());
    }
}
