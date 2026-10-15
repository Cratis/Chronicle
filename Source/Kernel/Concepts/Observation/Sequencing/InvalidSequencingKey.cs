// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// The exception that is thrown when a sequencing key has invalid parts or a missing identity.
/// </summary>
/// <param name="reason">The reason the key is invalid.</param>
public sealed class InvalidSequencingKey(string reason) : Exception($"Invalid sequencing key: {reason}");
