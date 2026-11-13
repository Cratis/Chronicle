// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Represents a durable append intention and its observed outcome.
/// </summary>
/// <param name="Id">The intention identifier.</param>
/// <param name="Source">The event source.</param>
/// <param name="Payload">The intended contribution.</param>
/// <param name="Status">The observed append outcome.</param>
internal sealed record LedgerEntry(string Id, string Source, long Payload, string Status);
