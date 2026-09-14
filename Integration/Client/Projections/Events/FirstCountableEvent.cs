// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Events;

[EventType("0d6f8b24-0b3d-4e9f-9c4a-5e8b7f1a2c3d")]
public record FirstCountableEvent(string Description);
