// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an exact condition cannot protect declared routing.
/// </summary>
/// <param name="label">The condition label.</param>
/// <param name="description">The routing definition or stream.</param>
public class DeclaredRoutingNotCoveredByExplicitScope(EventSourceId label, string description)
    : Exception($"The command's exact scope for '{label}' does not cover the declared concurrency of {description}. Widen the scope, or remove the declared concurrency.");
