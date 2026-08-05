// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents the outcome for one declared client artifact within a <see cref="RegistrationOutcome"/>.
/// </summary>
/// <param name="ArtifactType">
/// The type that was declared - the <see cref="Projections.IProjectionFor{TReadModel}"/> implementation for a fluent
/// projection, the read model type for a model-bound one.
/// </param>
/// <param name="Failure">
/// The <see cref="Exception"/> that stopped the artifact from being built, or <see langword="null"/> when it registered.
/// </param>
public record ArtifactRegistration(Type ArtifactType, Exception? Failure)
{
    /// <summary>
    /// Gets a value indicating whether the artifact registered.
    /// </summary>
    public bool IsRegistered => Failure is null;
}
