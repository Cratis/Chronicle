// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the materialization binding.
/// </summary>
/// <param name="Key">The key.</param>
/// <param name="Container">The container.</param>
/// <param name="Active">The active.</param>
/// <param name="Destination">The destination.</param>
/// <param name="Epoch">The epoch.</param>
/// <param name="Next">The next.</param>
/// <param name="Fence">The fence.</param>
/// <param name="LastReceipt">The last receipt.</param>
/// <param name="Generations">The generations.</param>
public record MaterializationBinding(MaterializationKey Key, ReadModelContainerName Container, MaterializationGeneration Active, ReadModelContainerName Destination, ServingEpoch Epoch, MaterializationGeneration Next, MaterializationFence? Fence, PublicationReceiptId? LastReceipt, IReadOnlyList<MaterializationGenerationRecord> Generations);
