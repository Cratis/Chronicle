// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence window limit is not positive.
/// </summary>
/// <param name="limit">The invalid limit.</param>
public class InvalidEventSequenceWindowLimit(int limit) : Exception($"The event sequence window limit must be positive, but was {limit}.");
