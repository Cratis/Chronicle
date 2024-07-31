// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.XUnit;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for testing.
/// </summary>
public class ChronicleConnectionForTesting : IChronicleConnection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnectionForTesting"/> class.
    /// </summary>
    public ChronicleConnectionForTesting()
    {
        Lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
    }

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; }

    /// <inheritdoc/>
    public IServices Services => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
