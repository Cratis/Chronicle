// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor.given;

/// <summary>
/// What a released kernel did when started over a populated store.
/// </summary>
/// <param name="SeededStores">The number of event-store databases that received a floor.</param>
/// <param name="Exited">Whether the kernel exited instead of staying up.</param>
/// <param name="NamedTheFloor">Whether the kernel's output named the generation floor as the reason.</param>
/// <param name="Writes">The writes the witness recorded against event-store databases.</param>
/// <param name="CreatedDatabases">The event-store databases created after the witness was armed.</param>
public record KernelOutcome(int SeededStores, bool Exited, bool NamedTheFloor, long Writes, int CreatedDatabases);
