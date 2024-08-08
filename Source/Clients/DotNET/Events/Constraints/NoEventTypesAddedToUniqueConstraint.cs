// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an event type has already been added to the unique constraint.
/// </summary>
public class NoEventTypesAddedToUniqueConstraint() : Exception("No event types have been added to the unique constraint");
