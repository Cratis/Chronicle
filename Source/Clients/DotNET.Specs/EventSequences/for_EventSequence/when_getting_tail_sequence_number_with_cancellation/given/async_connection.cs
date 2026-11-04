// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation.given;

public sealed class async_connection(IChronicleConnection connection, IChronicleServicesAccessor accessor) : IChronicleConnection, IChronicleServicesAccessor, IGetServicesAsynchronously
{
    public required Func<CancellationToken, ValueTask<IServices>> Acquire { get; set; }
    public int AcquisitionCalls { get; private set; }
    public IConnectionLifecycle Lifecycle => connection.Lifecycle;
    IServices IChronicleServicesAccessor.Services => accessor.Services;

    public Task Connect() => connection.Connect();

    public void RequestReconnect() => connection.RequestReconnect();

    public void Dispose() => connection.Dispose();

    public ValueTask<IServices> GetServices(CancellationToken cancellationToken)
    {
        AcquisitionCalls++;
        return Acquire(cancellationToken);
    }
}
