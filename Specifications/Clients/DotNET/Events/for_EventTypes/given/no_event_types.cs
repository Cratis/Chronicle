// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.given;

public class no_event_types : Specification
{
    protected Mock<ITypes> types;
    protected EventTypes event_types;

    void Establish()
    {
        types = new();
        event_types = new EventTypes(types.Object);
    }
}
