// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios;

[EventType("6ac8fdc7-ddc7-451b-8a7c-f36bd1e18a7b")]
public record EventWithChildObject(Model Child)
{
    public static EventWithChildObject Create() => new(Model.CreateWithRandomValues());
}
