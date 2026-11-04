// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The columns of a read model table and the ones forming its primary key, as the database catalog spells them.
/// </summary>
/// <param name="PrimaryKey">The primary key column names, in no particular order. Empty when the table does not exist.</param>
/// <param name="Columns">The names of all columns. Empty when the table does not exist.</param>
internal record TableLayout(IReadOnlyList<string> PrimaryKey, IReadOnlyList<string> Columns);
