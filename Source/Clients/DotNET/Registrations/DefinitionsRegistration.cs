// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents the definitions registered by a deployment step.
/// </summary>
/// <param name="Fingerprint">The fingerprint of the prepared definitions.</param>
/// <param name="Generation">The deployment generation sent to the kernel.</param>
public record DefinitionsRegistration(ArtifactFingerprint Fingerprint, DeploymentGeneration Generation);
