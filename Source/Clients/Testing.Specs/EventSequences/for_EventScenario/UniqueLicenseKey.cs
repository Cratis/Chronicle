// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A fluent <see cref="IConstraint"/> enforcing a single <see cref="LicenseKey"/> (a <c>ConceptAs&lt;Guid&gt;</c>)
/// across both <see cref="LicenseIssued"/> and <see cref="LicenseReissued"/>.
/// </summary>
public class UniqueLicenseKey : IConstraint
{
    /// <summary>
    /// The name of the constraint.
    /// </summary>
    public const string Name = "UniqueLicenseKey";

    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) =>
        builder.Unique(_ => _
            .On<LicenseIssued>(e => e.Key)
            .On<LicenseReissued>(e => e.Key)
            .WithName(Name));
}
