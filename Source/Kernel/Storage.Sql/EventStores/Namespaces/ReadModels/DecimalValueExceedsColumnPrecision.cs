// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The exception that is thrown when a decimal cannot be stored losslessly in a SQL Server read-model column.
/// </summary>
/// <param name="value">The decimal that exceeds DECIMAL(38,18).</param>
public sealed class DecimalValueExceedsColumnPrecision(decimal value) : Exception($"Decimal value {value} cannot be stored losslessly in a DECIMAL(38,18) read-model column.");
