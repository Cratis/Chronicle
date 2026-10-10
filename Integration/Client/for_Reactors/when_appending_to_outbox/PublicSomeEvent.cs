// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Reactors.when_appending_to_outbox;

/// <summary>
/// The public counterpart of the private event forwarded by the reactor to the outbox.
/// </summary>
/// <param name="Number">The number carried by the private event.</param>
[EventType]
[Public]
public record PublicSomeEvent(int Number);
