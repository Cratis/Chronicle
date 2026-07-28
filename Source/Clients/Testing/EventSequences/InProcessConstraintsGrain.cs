// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelConstraintConcepts = KernelConcepts::Cratis.Chronicle.Concepts.Events.Constraints;
using KernelConstraints = KernelCore::Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// A no-op kernel <see cref="KernelConstraints::IConstraints"/> grain for in-process scenarios.
/// </summary>
/// <remarks>
/// The kernel event sequence grain checks the constraints version on every append to pick up constraints registered
/// while it is active. In-process there is no constraints grain and the registered constraints are injected directly
/// into the event sequence grain and never change during a scenario, so this reports the unset version — which matches
/// the grain's cached version — making the check a no-op and leaving the injected validators in force.
/// </remarks>
internal sealed class InProcessConstraintsGrain : KernelConstraints::IConstraints
{
    /// <inheritdoc/>
    public Task Register(IEnumerable<KernelConstraintConcepts::IConstraintDefinition> definitions) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<KernelConstraintConcepts::IConstraintDefinition>> GetDefinitions() =>
        Task.FromResult<IReadOnlyCollection<KernelConstraintConcepts::IConstraintDefinition>>([]);

    /// <inheritdoc/>
    public Task<KernelConstraintConcepts::ConstraintsVersion> GetVersion() =>
        Task.FromResult(KernelConstraintConcepts::ConstraintsVersion.NotSet);
}
