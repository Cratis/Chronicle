// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// The exception that is thrown when the released kernel does not become ready, naming the wait that was still pending.
/// </summary>
/// <param name="activeWait">The readiness wait that had not been satisfied.</param>
/// <param name="containerState">The kernel container state when the timeout was observed.</param>
/// <param name="stdout">The tail of the kernel's standard output.</param>
/// <param name="stderr">The tail of the kernel's standard error.</param>
/// <param name="innerException">The readiness timeout.</param>
public sealed class ReleasedKernelNotReady(string activeWait, string containerState, string stdout, string stderr, Exception innerException)
    : Exception(
        $"Released kernel not ready: waiting on {activeWait}; container state {containerState}.\n--- stdout (tail) ---\n{stdout}\n--- stderr (tail) ---\n{stderr}",
        innerException);
