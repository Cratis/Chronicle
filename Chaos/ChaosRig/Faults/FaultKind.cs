// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Identifies a fault target and whether it is killed or paused.
/// </summary>
internal enum FaultKind
{
    /// <summary>
    /// Kills the kernel container.
    /// </summary>
    KillKernel = 0,

    /// <summary>
    /// Kills the kernel storage container.
    /// </summary>
    KillMongo = 1,

    /// <summary>
    /// Pauses the kernel container.
    /// </summary>
    StopKernel = 2,

    /// <summary>
    /// Pauses the kernel storage container.
    /// </summary>
    StopMongo = 3,

    /// <summary>
    /// Kills the producer process.
    /// </summary>
    KillProducer = 4,

    /// <summary>
    /// Stops the producer process with a signal.
    /// </summary>
    StopProducer = 5,

    /// <summary>
    /// Kills the consumer process.
    /// </summary>
    KillConsumer = 6,

    /// <summary>
    /// Stops the consumer process with a signal.
    /// </summary>
    StopConsumer = 7
}
