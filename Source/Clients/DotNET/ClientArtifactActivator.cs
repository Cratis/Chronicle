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
[IgnoreConvention]
public class ClientArtifactActivator(IServiceProvider rootServiceProvider, ILoggerFactory loggerFactory)
    : IClientArtifactsActivator
{
    readonly ILogger _logger = loggerFactory.CreateLogger<ClientArtifactActivator>();

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> Activate(Type artifactType) => Activate(rootServiceProvider, artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> Activate(IServiceProvider scopedServiceProvider, Type artifactType) =>
        ActivateWithLogging<ActivatedArtifact>(artifactType, () =>
        {
            var instance = ActivatorUtilities.GetServiceOrCreateInstance(scopedServiceProvider, artifactType);
            return new ActivatedArtifact(instance, artifactType, loggerFactory);
        });

    /// <inheritdoc/>
    public Catch<ActivatedArtifact<T>> Activate<T>(Type artifactType)
        where T : class => Activate<T>(rootServiceProvider, artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact<T>> Activate<T>(IServiceProvider scopedServiceProvider, Type artifactType)
        where T : class =>
        ActivateWithLogging<ActivatedArtifact<T>>(artifactType, () =>
        {
            if (ActivatorUtilities.GetServiceOrCreateInstance(scopedServiceProvider, artifactType) is T instance)
            {
                return new ActivatedArtifact<T>(instance, loggerFactory);
            }

            var message = $"Failed to activate artifact of type {artifactType.FullName} as {typeof(T).FullName}.";
            return new ClientArtifactActivationFailed(artifactType, new InvalidCastException(message));
        });

    /// <inheritdoc/>
    public Catch<object> ActivateNonDisposable(Type artifactType) =>
        ActivateWithLogging<object>(artifactType, () =>
        {
            var instance = ActivatorUtilities.GetServiceOrCreateInstance(rootServiceProvider, artifactType);
            return instance is IDisposable or IAsyncDisposable
                ? new ClientArtifactIsDisposable(artifactType)
                : instance;
        });

    /// <inheritdoc/>
    public Catch<T> ActivateNonDisposable<T>(Type artifactType)
        where T : class =>
        ActivateWithLogging<T>(artifactType, () =>
        {
            if (ActivatorUtilities.GetServiceOrCreateInstance(rootServiceProvider, artifactType) is T instance)
            {
                return instance is IDisposable or IAsyncDisposable
                    ? new ClientArtifactIsDisposable(artifactType)
                    : instance;
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
