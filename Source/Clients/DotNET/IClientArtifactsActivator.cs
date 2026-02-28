// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle;

/// <summary>
/// Defines a system that can activate artifact instances using a service provider.
/// </summary>
public interface IClientArtifactsActivator
{
    /// <summary>
    /// Creates an instance of the specified artifact type using the root service provider.
    /// </summary>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    Catch<ActivatedArtifact> Activate(Type artifactType);

    /// <summary>
    /// Creates an instance of the specified artifact type using the root service provider.
    /// </summary>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    /// <typeparam name="T">The type of the artifact to create.</typeparam>
    Catch<ActivatedArtifact<T>> Activate<T>(Type artifactType)
        where T : class;

    /// <summary>
    /// Creates an instance of the specified artifact type using the root service provider.
    /// </summary>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    Catch<object> ActivateNonDisposable(Type artifactType);

    /// <summary>
    /// Creates an instance of the specified artifact type using the root service provider.
    /// </summary>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    /// <typeparam name="T">The type of the artifact to create.</typeparam>
    Catch<T> ActivateNonDisposable<T>(Type artifactType)
        where T : class;

    /// <summary>
    /// Creates an instance of the specified artifact type using the given service provider.
    /// </summary>
    /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/> for resolving dependencies.</param>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    Catch<ActivatedArtifact> Activate(IServiceProvider scopedServiceProvider, Type artifactType);

    /// <summary>
    /// Creates an instance of the specified artifact type using the given service provider.
    /// </summary>
    /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/> for resolving dependencies.</param>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    /// <typeparam name="T">The type of the artifact to create.</typeparam>
    Catch<ActivatedArtifact<T>> Activate<T>(IServiceProvider scopedServiceProvider, Type artifactType)
        where T : class;
}
