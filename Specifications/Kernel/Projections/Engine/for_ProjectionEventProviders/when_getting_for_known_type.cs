// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.for_ProjectionEventProviders
{
    public class when_getting_for_known_type : Specification
    {
        static ProjectionEventProviderTypeId known_type = "2a57bce3-ef22-4bc6-932c-5df0ab606209";
        ProjectionEventProviders providers;
        Mock<IProjectionEventProvider> provider;
        IProjectionEventProvider result;

        void Establish()
        {
            provider = new Mock<IProjectionEventProvider>();
            provider.SetupGet(_ => _.TypeId).Returns(known_type);
            providers = new(new KnownInstancesOf<IProjectionEventProvider>(new[] { provider.Object }));
        }

        void Because() => result = providers.GetForType(known_type);

        [Fact] void should_return_the_known_provider() => result.ShouldEqual(provider.Object);
    }
}
