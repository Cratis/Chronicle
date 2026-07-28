// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[EventType("c0ffee00-0000-4000-8000-000000000001")]
public record WatchedEvent(int Number);
