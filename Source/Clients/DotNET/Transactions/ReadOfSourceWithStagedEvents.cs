// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a read cannot observe matching events already staged in this command.
/// </summary>
/// <param name="label">The condition label.</param>
/// <param name="description">The read description.</param>
public class ReadOfSourceWithStagedEvents(EventSourceId label, string description) : Exception($"The command read '{label}' ({description}) after events for it were staged in this command; the read cannot see them. Decide once in the outer command and pass the value down.");
