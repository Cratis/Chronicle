// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Represents an event read from the persisted event log.
/// </summary>
/// <param name="Id">The intention identifier.</param>
/// <param name="Source">The event source.</param>
/// <param name="Sequence">The persisted sequence number.</param>
/// <param name="Payload">The persisted contribution.</param>
internal sealed record ObservedEvent(string Id, string Source, ulong Sequence, long Payload);
