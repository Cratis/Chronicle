// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Explicitly associates an opaque observer identity with immutable generation execution.
/// </summary>
/// <param name="Key">The materialization, including store incarnation and namespace.</param>
/// <param name="Identity">The reserved generation, observer and destination.</param>
/// <param name="EventSequence">The source event sequence.</param>
/// <param name="Revision">The immutable execution revision.</param>
public sealed record GenerationObserverBinding(MaterializationKey Key, MaterializationGenerationIdentity Identity, EventSequenceId EventSequence, MaterializationRevision Revision);
