// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias Server;
extern alias Client;
using Client::Cratis.Chronicle;
using Client::Cratis.Chronicle.Connections;

namespace Orleans.Hosting;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for Orleans in-process.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleConnection"/> class.
/// </remarks>
/// <param name="lifecycle"><see cref="IConnectionLifecycle"/> for managing lifecycle.</param>
/// <param name="services"><see cref="IServices"/> to use.</param>
public class ChronicleConnection(IConnectionLifecycle lifecycle, IServices services) : IChronicleConnection
{
    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; } = lifecycle;

    /// <inheritdoc/>
    public IServices Services { get; } = services;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
