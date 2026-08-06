// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when a removal event is declared without a unique event type constraint to release.
/// </summary>
public class NoUniqueEventTypeConstraintToRemove()
    : Exception("RemovedWith<TRemovalEventType>() releases the unique event type constraint it follows, and no Unique<TEventType>() has been declared on the builder");
