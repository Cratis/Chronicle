// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Api.for_EventTypes;

[EventType("26f57829-c3a0-45ca-b1fc-2e05e6e54b8e")]
public record TestEvent(string Content);
