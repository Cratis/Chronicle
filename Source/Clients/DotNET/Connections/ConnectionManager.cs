// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Connections;

/// <summary>
/// Represents an implementation of <see cref="IConnectionManager"/>.
/// </summary>
[Singleton]
public class ConnectionManager : IConnectionManager
{
    /// <summary>
    /// The string identifying a kernel internal connection.
    /// </summary>
    public const string KernelConnectionId = "kernel-internal";

    internal static string InternalConnectionId = "[not set]";

    bool _isKernel;

    /// <inheritdoc/>
    public string ConnectionId => _isKernel ? KernelConnectionId : InternalConnectionId;

    /// <inheritdoc/>
    public void SetKernelMode()
    {
        _isKernel = true;
    }
}
