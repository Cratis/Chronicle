// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.for_ProjectionEventProviders
{
    public class when_getting_for_unknown_type : Specification
    {
        ProjectionEventProviders providers;
        Exception result;

        void Establish() => providers = new(new KnownInstancesOf<IProjectionEventProvider>(Array.Empty<IProjectionEventProvider>()));

        void Because() => result = Catch.Exception(() => providers.GetForType("62cd16f7-3b9e-483f-96cc-679eb525e622"));

        [Fact] void should_throw_unknown_projection_event_provider() => result.ShouldBeOfExactType<UnknownProjectionEventProvider>();
    }
}
