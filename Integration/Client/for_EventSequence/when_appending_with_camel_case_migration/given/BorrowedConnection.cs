// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;

internal sealed class BorrowedConnection(IChronicleConnection connection) : IChronicleConnection, IChronicleServicesAccessor
{
    public IConnectionLifecycle Lifecycle => connection.Lifecycle;
    public IServices Services => ((IChronicleServicesAccessor)connection).Services;
    public Task Connect() => connection.Connect();

    public void Dispose()
    {
        // The integration fixture owns the connection and keeps it alive across scenarios.
    }
}
