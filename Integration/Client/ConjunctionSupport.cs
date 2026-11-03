// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Integration;

internal static class ConjunctionSupport
{
    internal static IDisposable Withhold(IEventStore store) =>
        ((XUnit.Integration.ChronicleConnection)store.Connection).WithholdCapabilities(KernelCapabilities.ConcurrencyScopeConjunctions);

    internal static bool IsAdvertised(IEventStore store) =>
        store.Connection is IKernelCapabilities { CapabilitiesAreKnown: true } capabilities &&
        capabilities.Capabilities.Contains(KernelCapabilities.ConcurrencyScopeConjunctions);
}
