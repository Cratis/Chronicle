// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception thrown when a deployment generation exceeds signed storage capacity.
/// </summary>
/// <param name="generation">The rejected generation.</param>
public class DeploymentGenerationOutOfRange(DeploymentGeneration generation)
    : Exception($"Deployment generation '{generation}' exceeds the maximum of {long.MaxValue} (long.MaxValue).");
