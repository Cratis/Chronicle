// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence provider cannot atomically deduplicate publications.
/// </summary>
public class EventPublicationStorageNotSupported() : Exception("The event sequence storage does not support atomic publication identities.");
