// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a contributed read is neither explicit nor an aggregate.
/// </summary>
/// <param name="origin">The invalid origin.</param>
public class ContributedReadOriginNotValid(ConcurrencyScopeOrigin origin) : Exception($"Contributed read origin '{origin}' must be Explicit or Aggregate.");
