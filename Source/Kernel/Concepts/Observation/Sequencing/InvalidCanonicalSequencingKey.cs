// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// The exception that is thrown when stored bytes are not a canonical sequencing key.
/// </summary>
/// <param name="reason">The reason the stored bytes are invalid.</param>
public sealed class InvalidCanonicalSequencingKey(string reason) : Exception($"Invalid canonical sequencing key: {reason}");
