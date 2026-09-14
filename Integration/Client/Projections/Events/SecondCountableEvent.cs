// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Events;

[EventType("1e7f9c35-1c4e-5faf-ad5b-6f9c8a2b3d4e")]
public record SecondCountableEvent(string Description);
