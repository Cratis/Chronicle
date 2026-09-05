// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the persisted command envelope for an event sequence mutation.
/// </summary>
/// <param name="Payload">The serialized mutation command payload.</param>
/// <param name="Hash">The hash of the mutation command.</param>
public sealed record EventSequenceMutationCommandEnvelope(string Payload, EventSequenceMutationCommandHash Hash);
