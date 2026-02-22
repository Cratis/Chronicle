// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a wrapper around an activated artifact instance that handles disposal.
/// </summary>
/// <param name="instance">The activated artifact instance.</param>
/// <param name="artifactType">The <see cref="Type"/> of the artifact.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
public class ActivatedArtifact(object instance, Type artifactType, ILogger<ActivatedArtifact> logger) : IAsyncDisposable
{
    /// <summary>
    /// Gets the activated artifact instance.
    /// </summary>
    public object Instance { get; } = instance;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        logger.DisposingArtifact(artifactType);
        try
        {
            if (Instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.FailedDisposingArtifact(artifactType, ex);
        }
    }
}
