// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system that discovers <see cref="IReadModelReactor"/> implementations and subscribes them to read
/// model changes.
/// </summary>
public interface IReadModelReactors : IDisposable
{
    /// <summary>
    /// Discover all read model reactors and start watching the read models they react to.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop watching and dispose of all subscriptions.
    /// </summary>
    void Stop();
}
