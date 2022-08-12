// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.when_asking_if_has_event_type_for_clr_type;

public class and_there_is_no_mapping : given.no_event_types
{
    bool result;

    void Because() => result = event_types.HasFor(typeof(MyEvent));

    [Fact] void should_not_have_mapping() => result.ShouldBeFalse();
}
