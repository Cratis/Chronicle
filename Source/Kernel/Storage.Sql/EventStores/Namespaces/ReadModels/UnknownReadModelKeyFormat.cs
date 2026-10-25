// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Exception that gets thrown when a stored key format is not known to this kernel version.
/// </summary>
/// <param name="tableName">The physical table the format was recorded for.</param>
/// <param name="format">The stored format value.</param>
public class UnknownReadModelKeyFormat(string tableName, string format)
    : Exception($"Read model table '{tableName}' has the key format '{format}', which this kernel version does not understand. Upgrade the kernel.");
