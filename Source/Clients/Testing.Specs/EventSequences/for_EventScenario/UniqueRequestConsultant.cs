// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A fluent <see cref="IConstraint"/> enforcing uniqueness of the composite (request + consultant)
/// key, where both parts are <c>ConceptAs&lt;Guid&gt;</c> values.
/// </summary>
public class UniqueRequestConsultant : IConstraint
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "UniqueRequestConsultant";

    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(_ => _
            .On<CandidateSubmittedForRequest>(e => e.Request, e => e.Consultant)
            .WithName(Name));
}
