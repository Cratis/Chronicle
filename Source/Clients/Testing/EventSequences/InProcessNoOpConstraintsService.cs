// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op implementation of <see cref="IConstraints"/> (contract) used in test scenarios.
/// </summary>
/// <remarks>
/// Constraint registration is handled directly by the kernel grain via <see cref="InMemoryConstraintsStorage"/>,
/// so this service implementation is intentionally a no-op.
/// </remarks>
internal sealed class InProcessNoOpConstraintsService : IConstraints
{
    /// <inheritdoc/>
    public Task Register(RegisterConstraintsRequest request) =>
        Task.CompletedTask;
}
