// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a system for working with <see cref="RecoverFailedPartitionState"/>.
/// </summary>
public interface IFailedPartitionsState
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="RecoverFailedPartitionState"/>.
    /// </summary>
    IObservable<IEnumerable<RecoverFailedPartitionState>> All { get; }
}
