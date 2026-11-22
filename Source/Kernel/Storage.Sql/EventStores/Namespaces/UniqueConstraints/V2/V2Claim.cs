// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Stores a claim without narrowing its unsigned sequence or nullable subject.
/// </summary>
/// <param name="Scope">The encoded scope.</param>
/// <param name="EventSourceId">The bounded owner.</param>
/// <param name="Value">The encoded value.</param>
/// <param name="SequenceNumber">The first unsigned sequence, stored losslessly as a decimal.</param>
/// <param name="Subject">The full subject, or null.</param>
sealed record V2Claim(string Scope, string EventSourceId, string Value, decimal SequenceNumber, string? Subject);
