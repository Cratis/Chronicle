// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load;

/// <summary>
/// The state a silo reports about how busy it is.
/// </summary>
public enum KernelLoadStatus
{
    /// <summary>
    /// The silo is under every configured limit and has been for a while.
    /// </summary>
    Accepting = 0,

    /// <summary>
    /// The silo has crossed a configured limit: calls to grains are queueing or waiting longer than they should.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The silo is back under every limit, but not for long enough yet to be called accepting.
    /// </summary>
    Recovering = 2
}
