// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents a wrapper around an activated artifact instance that handles disposal.
/// </summary>
/// <typeparam name="T">The type of the activated artifact.</typeparam>
/// <param name="instance">The activated artifact instance.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for creating loggers.</param>
public class ActivatedArtifact<T>(T instance, ILoggerFactory loggerFactory)
    : ActivatedArtifact(instance, typeof(T), loggerFactory)
    where T : class
{
    /// <summary>
    /// Gets the activated artifact instance strongly typed.
    /// </summary>
    public new T Instance { get; } = instance;
}
