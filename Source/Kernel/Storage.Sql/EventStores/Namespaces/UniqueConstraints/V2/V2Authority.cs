// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Persists the full logical authority tuple beside its fixed-size lookup key.
/// </summary>
/// <param name="Id">The tuple digest.</param>
/// <param name="Sequence">The full sequence identity.</param>
/// <param name="Constraint">The full constraint identity.</param>
/// <param name="Epoch">The epoch.</param>
/// <param name="IsLive">Whether the phase is Live, distinct from every run.</param>
/// <param name="RunId">The run, or null for Live.</param>
/// <param name="Revision">The nonnegative monotonic revision.</param>
sealed record V2Authority(string Id, string Sequence, string Constraint, Guid Epoch, bool IsLive, Guid? RunId, long Revision);
