// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.given;

public class one_event_type : no_event_types
{
    void Establish()
    {
        types.SetupGet(_ => _.All).Returns(new[] { typeof(MyEvent) });
        event_types = new EventTypes(types.Object);
    }
}
