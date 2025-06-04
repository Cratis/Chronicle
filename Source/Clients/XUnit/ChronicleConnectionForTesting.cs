// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.XUnit;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for testing.
/// </summary>
internal sealed class ChronicleConnectionForTesting : IChronicleConnection, IChronicleServicesAccessor
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
    IServices IChronicleServicesAccessor.Services => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
