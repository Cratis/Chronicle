// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Holds the value of an exact comparison so it travels to the query as a parameter.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <param name="Value">The value to match.</param>
/// <param name="Bytes">The length of the value in bytes.</param>
sealed record ExactValue<TValue>(TValue Value, int Bytes);
