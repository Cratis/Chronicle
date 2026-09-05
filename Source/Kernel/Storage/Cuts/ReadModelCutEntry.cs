// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// Represents the outcome and, on success, the payload location for one read model in a <see cref="ReadModelCutManifest"/>.
/// </summary>
/// <param name="ReadModel">The read model identifier this entry is for.</param>
/// <param name="Outcome">The <see cref="ReadModelCutOutcome"/> for this read model.</param>
/// <param name="Generation">
/// The read-model schema generation the payload was produced under, when <paramref name="Outcome"/> is
/// <see cref="ReadModelCutOutcome.Captured"/>. This is the current generation, not a reconstructed historical
/// one - there is no projection-definition-history store this can be pinned against, so it is recorded for
/// honest provenance rather than as proof of a point-in-time schema.
/// </param>
/// <param name="Digest">The <see cref="ReadModelCutPayloadDigest"/> of the payload, when <paramref name="Outcome"/> is <see cref="ReadModelCutOutcome.Captured"/>.</param>
/// <param name="FailureReason">A human-readable reason, when <paramref name="Outcome"/> is not <see cref="ReadModelCutOutcome.Captured"/>.</param>
public sealed record ReadModelCutEntry(
    ReadModelIdentifier ReadModel,
    ReadModelCutOutcome Outcome,
    ReadModelGeneration? Generation,
    ReadModelCutPayloadDigest? Digest,
    string? FailureReason);
