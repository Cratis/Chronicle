// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// A hold a step records once the delivered prefix has succeeded, because the next eligible event has no capable target.
/// </summary>
/// <param name="AwaitedEventType">The event type nothing can handle yet.</param>
/// <param name="Cursor">The exact position and mode from which recovery resumes.</param>
internal sealed record PendingTypeSetHold(EventTypeId AwaitedEventType, FailedPartitionRecoveryCursor Cursor);
