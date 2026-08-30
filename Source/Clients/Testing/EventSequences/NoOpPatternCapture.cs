// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelCore::Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op <see cref="IPatternCapture"/> for the single-process test harness.
/// </summary>
/// <remarks>
/// Capture subscribes an observer against a silo, and the test harness has no silo to subscribe against - it stands
/// the kernel's services up in-process rather than running a server. Behavior patterns are therefore **not** mined
/// in the test harness, and a spec asserting on them will find nothing however many events it appends. Verifying
/// pattern detection needs a running kernel.
/// </remarks>
internal sealed class NoOpPatternCapture : IPatternCapture
{
    /// <inheritdoc/>
    public Task Subscribe(
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore,
        KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName @namespace) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task SubscribeAcrossNamespaces(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName eventStore) => Task.CompletedTask;
}
