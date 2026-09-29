// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines how an error raised while talking to the kernel should be treated by an observer on the client.
/// </summary>
internal enum KernelConnectionErrorKind
{
    /// <summary>
    /// A genuine failure that is not explained by the connection to the kernel going away.
    /// </summary>
    Failure = 0,

    /// <summary>
    /// The call was cancelled or disposed, typically because the client or host is shutting down.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// The kernel is stopping or unreachable - a transient loss of connection the client recovers from.
    /// </summary>
    ConnectionLost = 2
}
