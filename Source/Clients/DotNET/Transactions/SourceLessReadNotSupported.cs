// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a contributed read does not narrow by source.
/// </summary>
/// <param name="description">The read description.</param>
public class SourceLessReadNotSupported(string description) : Exception($"Read protection cannot enroll source-less read '{description}'. Read a source-keyed model or declare the read advisory.");
