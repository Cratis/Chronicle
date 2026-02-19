// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Defines a system that can activate artifact instances using a service provider.
/// </summary>
public interface IArtifactActivator
{

    /// <summary>
    /// Creates an instance of the specified artifact type using the root service provider.
    /// </summary>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    ActivatedArtifact CreateInstance(Type artifactType);

    /// <summary>
    /// Creates an instance of the specified artifact type using the given service provider.
    /// </summary>
    /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/> for resolving dependencies.</param>
    /// <param name="artifactType">The <see cref="Type"/> of the artifact to create.</param>
    /// <returns>An <see cref="ActivatedArtifact"/> wrapping the created instance.</returns>
    ActivatedArtifact CreateInstance(IServiceProvider scopedServiceProvider, Type artifactType);
}
