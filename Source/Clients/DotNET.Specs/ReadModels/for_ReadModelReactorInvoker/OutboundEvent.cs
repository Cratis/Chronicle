// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorInvoker;

[EventType]
public record OutboundEvent(string Something = "something");
