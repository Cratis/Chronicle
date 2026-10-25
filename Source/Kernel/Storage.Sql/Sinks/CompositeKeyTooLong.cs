// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The exception that is thrown when the encoded composite key does not fit the key column.
/// </summary>
/// <param name="tableName">The table the key was written to.</param>
/// <param name="length">The length of the encoded key.</param>
public class CompositeKeyTooLong(string tableName, int length)
    : Exception($"The encoded composite key for read model table '{tableName}' is {length} characters, which exceeds the limit of {Sink.MaxCompositeKeyLength}.");
