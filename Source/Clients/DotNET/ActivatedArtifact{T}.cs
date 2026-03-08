// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a wrapper around an activated artifact instance that handles disposal.
/// </summary>
/// <typeparam name="T">The type of the activated artifact.</typeparam>
/// <param name="instance">The activated artifact instance.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
public class ActivatedArtifact<T>(T instance, ILogger<ActivatedArtifact> logger)
    : ActivatedArtifact(instance, typeof(T), logger)
    where T : class
{
    /// <summary>
    /// Gets the activated artifact instance strongly typed.
    /// </summary>
    public new T Instance { get; } = instance;
}
