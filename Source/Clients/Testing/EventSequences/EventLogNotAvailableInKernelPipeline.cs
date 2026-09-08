// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// The exception that is thrown when something calls a member of <see cref="NoOpEventLog"/> beyond
/// <see cref="NoOpEventLog.AppendOperations"/> - the kernel's own commands append directly through the grain and
/// never touch the client event log, so nothing should reach here.
/// </summary>
public class EventLogNotAvailableInKernelPipeline() : Exception(
    "The client event log is not available inside the in-process kernel command pipeline - kernel commands append through the grain directly.");
