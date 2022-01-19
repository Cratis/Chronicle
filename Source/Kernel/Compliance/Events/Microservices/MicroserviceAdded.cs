// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Compliance.Events.Microservices
{
    [EventType("431919dd-a0b0-4360-9299-31eed7dc811e")]
    public record MicroserviceAdded(string Name);
}
