// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Imports;

/// <summary>
/// Represents an event of an import that was not staged or not committed.
/// </summary>
/// <param name="Index">The index of the event in the import, as the client numbered it.</param>
/// <param name="EventType">The event type, as its identifier and generation.</param>
/// <param name="Reasons">Why the event was rejected.</param>
public record ImportRejection(long Index, string EventType, IEnumerable<string> Reasons);
