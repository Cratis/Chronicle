// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the sink and optional explicit occurrence addressed by a materialized read.
/// </summary>
/// <param name="Sink">The sink addressing the read model's physical container.</param>
/// <param name="Occurrence">The explicit occurrence, or null to retain the sink's primary-read semantics.</param>
public record ReadModelReadBinding(ISink Sink, ReadModelContainerName? Occurrence);
