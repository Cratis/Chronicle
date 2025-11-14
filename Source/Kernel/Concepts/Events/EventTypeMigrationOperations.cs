// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents a collection of operations for an event type migration.
/// </summary>
/// <param name="Operation">The <see cref="EventTypeMigrationOperation"/>.</param>
/// <param name="Details">The <see cref="IEventTypeMigrationOperationDetails"/>.</param>
public record EventTypeMigrationOperations(EventTypeMigrationOperation Operation, IEventTypeMigrationOperationDetails Details);
