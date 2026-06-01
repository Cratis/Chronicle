// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_completing_a_stream;

[EventType]
public record SomeStreamEvent(string Content);
