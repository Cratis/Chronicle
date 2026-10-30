// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts.Registrations;
using FluentValidation;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the validator for <see cref="ArtifactFingerprint"/>, applied to every command or query that carries one.
/// </summary>
public class ArtifactFingerprintValidator : ConceptValidator<ArtifactFingerprint>
{
    /// <summary>
    /// The maximum length of a fingerprint.
    /// </summary>
    public const int MaximumLength = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactFingerprintValidator"/> class.
    /// </summary>
    public ArtifactFingerprintValidator() => RuleFor(_ => _.Value).NotEmpty().MaximumLength(MaximumLength);
}
