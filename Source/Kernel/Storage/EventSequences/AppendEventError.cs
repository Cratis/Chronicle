// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Base class for append event errors.
/// </summary>
public abstract record AppendEventError;

/// <summary>
/// Error indicating a duplicate event sequence number was encountered.
/// </summary>
/// <param name="NextAvailableSequenceNumber">The next available sequence number that can be used.</param>
public record DuplicateEventSequenceNumberError(EventSequenceNumber NextAvailableSequenceNumber) : AppendEventError;
