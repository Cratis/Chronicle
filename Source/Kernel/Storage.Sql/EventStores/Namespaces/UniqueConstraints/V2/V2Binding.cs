// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Binds one complete logical tuple to an immutable physical table.
/// </summary>
/// <param name="Id">The logical tuple digest.</param>
/// <param name="Sequence">The full sequence identity.</param>
/// <param name="Constraint">The full constraint identity.</param>
/// <param name="Mode">The retention mode.</param>
/// <param name="Table">The physical table identity.</param>
sealed record V2Binding(string Id, string Sequence, string Constraint, UniqueConstraintMode Mode, string Table);
