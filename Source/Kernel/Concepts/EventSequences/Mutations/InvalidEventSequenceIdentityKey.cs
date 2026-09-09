// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when bytes do not form a strict UTF-8 event sequence identity key.
/// </summary>
public class InvalidEventSequenceIdentityKey() : Exception("The event sequence identity key must contain strict, round-trippable UTF-8 bytes.");
