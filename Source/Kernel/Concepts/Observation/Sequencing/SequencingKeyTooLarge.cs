// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// The exception that is thrown when a value sequencing key exceeds its canonical byte limit.
/// </summary>
public sealed class SequencingKeyTooLarge() : Exception($"A canonical sequencing key cannot exceed {CanonicalSequencingKey.MaximumLength} bytes.");
