// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.Generations;

/// <summary>A binary cannot admit a store whose durable generation floor it does not satisfy.</summary>
/// <param name="eventStore">The refused event store.</param>
/// <param name="floor">The durable floor.</param>
/// <param name="support">The executing binary's support.</param>
public class KernelBelowGenerationFloor(EventStoreName eventStore, GenerationFloor floor, GenerationBinarySupport support)
    : Exception($"Cannot admit event store '{eventStore}': this kernel is below generation floor revision {floor.Revision} (storage {floor.MinimumStorageProtocol}, writer {floor.MinimumWriterProtocol}); binary supports storage {support.Storage}, writer {support.Writer}.");
