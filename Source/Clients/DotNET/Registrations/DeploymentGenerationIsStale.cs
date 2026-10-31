// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception thrown when the kernel refuses an older deployment generation.
/// </summary>
/// <param name="eventStore">The event store being registered.</param>
/// <param name="generation">The refused generation.</param>
/// <param name="reason">The kernel's validation messages.</param>
public class DeploymentGenerationIsStale(EventStoreName eventStore, DeploymentGeneration generation, string reason)
    : Exception($"Deployment generation '{generation}' for event store '{eventStore}' is stale: {reason}");
