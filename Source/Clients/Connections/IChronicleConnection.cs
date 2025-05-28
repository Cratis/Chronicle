// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a system that manages the connection to Cratis.
/// </summary>
public interface IChronicleConnection : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IConnectionLifecycle"/> service.
    /// </summary>
    IConnectionLifecycle Lifecycle { get; }
}
