// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IClientArtifactsActivator"/>.
/// </summary>
/// <param name="rootServiceProvider">The root <see cref="IServiceProvider"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Singleton]
public class ClientArtifactActivator(IServiceProvider rootServiceProvider, ILoggerFactory loggerFactory)
    : IClientArtifactsActivator
{
    readonly ILogger<ClientArtifactActivator> _logger = loggerFactory.CreateLogger<ClientArtifactActivator>();
    readonly ILogger<ActivatedArtifact> _activatedArtifactLogger = loggerFactory.CreateLogger<ActivatedArtifact>();

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> Activate(Type artifactType) => Activate(rootServiceProvider, artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> Activate(IServiceProvider scopedServiceProvider, Type artifactType) =>
        ActivateWithLogging<ActivatedArtifact>(artifactType, () =>
        {
            var instance = ActivatorUtilities.GetServiceOrCreateInstance(scopedServiceProvider, artifactType);
            return new ActivatedArtifact(instance, artifactType, _activatedArtifactLogger);
        });

    /// <inheritdoc/>
    public Catch<ActivatedArtifact<T>> Activate<T>(Type artifactType)
        where T : class => Activate<T>(rootServiceProvider, artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact<T>> Activate<T>(IServiceProvider scopedServiceProvider, Type artifactType)
        where T : class =>
        ActivateWithLogging<ActivatedArtifact<T>>(artifactType, () =>
        {
            var instance = ActivatorUtilities.GetServiceOrCreateInstance(scopedServiceProvider, artifactType);
            if (instance is T typedInstance)
            {
                return new ActivatedArtifact<T>(typedInstance, _activatedArtifactLogger);
            }

            // If the instance cannot be cast to T, we should dispose it if it's disposable to avoid resource leaks.
            var activatedArtifact = new ActivatedArtifact(instance, artifactType, _activatedArtifactLogger);
            activatedArtifact.DisposeAsync().AsTask().GetAwaiter().GetResult();
            var message = $"Failed to activate artifact of type {artifactType.FullName} as {typeof(T).FullName}.";
            return new ClientArtifactActivationFailed(artifactType, new InvalidCastException(message));
        });

    /// <inheritdoc/>
    public Catch<object> ActivateNonDisposable(Type artifactType) =>
        ActivateWithLogging<object>(artifactType, () =>
        {
            if (typeof(IDisposable).IsAssignableFrom(artifactType) || typeof(IAsyncDisposable).IsAssignableFrom(artifactType))
            {
                return new ClientArtifactIsDisposable(artifactType);
            }
            return ActivatorUtilities.GetServiceOrCreateInstance(rootServiceProvider, artifactType);
        });

    /// <inheritdoc/>
    public Catch<T> ActivateNonDisposable<T>(Type artifactType)
        where T : class =>
        ActivateWithLogging<T>(artifactType, () =>
        {
            if (typeof(IDisposable).IsAssignableFrom(artifactType) || typeof(IAsyncDisposable).IsAssignableFrom(artifactType))
            {
                return new ClientArtifactIsDisposable(artifactType);
            }
            if (ActivatorUtilities.GetServiceOrCreateInstance(rootServiceProvider, artifactType) is T instance)
            {
                return instance;
            }

            var message = $"Failed to activate artifact of type {artifactType.FullName} as {typeof(T).FullName}.";
            return new ClientArtifactActivationFailed(artifactType, new InvalidCastException(message));
        });

    Catch<T> ActivateWithLogging<T>(Type artifactType, Func<Catch<T>> activationFunc)
        where T : class
    {
        _logger.ActivatingArtifact(artifactType);
        try
        {
            return activationFunc();
        }
        catch (Exception ex)
        {
            _logger.ArtifactActivationFailed(artifactType, ex);
            return new ClientArtifactActivationFailed(artifactType, ex);
        }
    }
}
