// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Api.for_EventTypes;

[EventType("8f7b4a3c-d2e1-4f9a-b8c7-1d6e3f2a4b5c")]
public record AnotherTestEvent(int Value);
