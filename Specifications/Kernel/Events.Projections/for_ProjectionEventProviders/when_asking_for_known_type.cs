// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Common;

namespace Cratis.Events.Projections.for_ProjectionEventProviders
{
    public class when_asking_for_known_type : Specification
    {
        static ProjectionEventProviderTypeId known_type = "2a57bce3-ef22-4bc6-932c-5df0ab606209";
        ProjectionEventProviders providers;
        Mock<IProjectionEventProvider> provider;
        bool result;

        void Establish()
        {
            provider = new Mock<IProjectionEventProvider>();
            provider.SetupGet(_ => _.TypeId).Returns(known_type);
            providers = new(new KnownInstancesOf<IProjectionEventProvider>(new[] { provider.Object }));
        }

        void Because() => result = providers.HasType(known_type);

        [Fact] void should_have_the_type() => result.ShouldBeTrue();
    }
}
