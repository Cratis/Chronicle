// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Contracts.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Kernel.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : IObservers
{
    /// <inheritdoc/>
    public Task RetryPartition(RetryPartitionRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Rewind(RewindRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task RewindPartition(RewindPartitionRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObservable<ObserverInformation> AllObservers(AllObserversRequest request, CallContext context = default) => throw new NotImplementedException();
}
