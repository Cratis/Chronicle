// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event whose <see cref="TallyReducer"/> handler records the event's sequence number, used to verify
/// the harness assigns increasing per-event sequence numbers rather than a constant.
/// </summary>
[EventType]
public record SequenceProbed;
