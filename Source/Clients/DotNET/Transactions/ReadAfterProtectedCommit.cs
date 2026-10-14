// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a protected read occurs after the invocation's unit completed.
/// </summary>
/// <param name="description">The read description.</param>
public class ReadAfterProtectedCommit(string description) : Exception($"The command read {description} after its changes were committed (aggregate Commit()). Read before committing, or return the result of Commit() last.");
