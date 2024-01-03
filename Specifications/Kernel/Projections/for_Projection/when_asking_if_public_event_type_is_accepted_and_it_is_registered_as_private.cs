// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.for_Projection;

public class when_asking_if_public_event_type_is_accepted_and_it_is_registered_as_private : given.a_projection
{
    static EventType event_type = new("993888cc-a9c5-4d56-ae21-f732159feec7", 1);
    bool result;

    void Establish()
    {
        projection.SetEventTypesWithKeyResolvers(
            new EventTypeWithKeyResolver[]
            {
                    new EventTypeWithKeyResolver(event_type, KeyResolvers.FromEventSourceId)
            },
            new[] { event_type });
    }

    void Because() => result = projection.Accepts(new EventType(event_type.Id, event_type.Generation, true));

    [Fact] void should_accept_it() => result.ShouldBeTrue();
}
