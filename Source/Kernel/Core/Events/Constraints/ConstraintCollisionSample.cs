// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a skipped duplicate claim without its value or event source identity.
/// </summary>
/// <param name="ValueHash">The lowercase hexadecimal HMAC-SHA256 of the claimed index value, keyed per step run.</param>
/// <param name="SequenceNumber">The sequence number of the skipped claim.</param>
/// <remarks>
/// Hashes are comparable only within one run and change after a resume. The random key is kept in memory only.
/// </remarks>
public record ConstraintCollisionSample(string ValueHash, EventSequenceNumber SequenceNumber);
