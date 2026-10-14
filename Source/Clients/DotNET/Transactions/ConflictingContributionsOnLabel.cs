// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when distinct explicit or aggregate conditions share a label.
/// </summary>
/// <param name="label">The condition label.</param>
/// <param name="first">The first contribution.</param>
/// <param name="second">The second contribution.</param>
public class ConflictingContributionsOnLabel(EventSourceId label, string first, string second)
    : Exception($"Contributions '{first}' and '{second}' conflict on label '{label}'; neither may overwrite the other.");
