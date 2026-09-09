// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the terminal cryptographic witness for an event sequence mutation.
/// </summary>
/// <param name="FinalStateVersion">The final persisted mutation state version.</param>
/// <param name="DefinitionDigestV1">The version 1 digest of the mutation definition.</param>
/// <param name="ReceiptDigestV1">The version 1 digest of the terminal receipt.</param>
public sealed record EventSequenceMutationTerminalWitness(
    EventSequenceMutationStateVersion FinalStateVersion,
    EventSequenceMutationDefinitionDigestV1 DefinitionDigestV1,
    EventSequenceMutationReceiptDigestV1 ReceiptDigestV1);
