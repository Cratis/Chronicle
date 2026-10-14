// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when distinct explicit conditions share a protected-read label.
/// </summary>
/// <param name="label">The condition label.</param>
/// <param name="first">The first contribution.</param>
/// <param name="second">The second contribution.</param>
public class ConflictingExplicitScopes(EventSourceId label, string first, string second)
    : Exception($"Explicit scopes '{first}' and '{second}' conflict on protected-read label '{label}'.");
