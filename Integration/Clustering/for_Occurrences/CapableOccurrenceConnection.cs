// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

/// <summary>
/// Adds the real kernel's handshake result to the existing in-process transport; no capabilities are invented.
/// </summary>
/// <param name="inner">The existing in-process connection.</param>
/// <param name="capabilities">The capabilities returned by its real kernel.</param>
public sealed class CapableOccurrenceConnection(IChronicleConnection inner, IReadOnlyCollection<string> capabilities) : IChronicleConnection, IChronicleServicesAccessor, IKernelCapabilities
{
    public IConnectionLifecycle Lifecycle => inner.Lifecycle;
    public IReadOnlyCollection<string> Capabilities => capabilities;
    public IServices Services => ((IChronicleServicesAccessor)inner).Services;
    public Task Connect() => inner.Connect();
    public void Dispose() => inner.Dispose();
}
