// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when a duplicate object leaves migrations pending.
/// </summary>
/// <param name="database">The physical database.</param>
/// <param name="migration">The first pending migration.</param>
/// <param name="cause">The duplicate-object failure.</param>
#pragma warning disable CA1064 // Kernel-only migration diagnostic, not a public client contract.
internal sealed class DatabaseMigrationIncomplete(string database, string migration, Exception cause)
    : Exception($"DatabaseMigrationIncomplete: database '{database}' still requires migration '{migration}' after an already-exists error.", cause);
