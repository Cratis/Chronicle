// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_type;

public class and_type_implements_pii_marker_interface : given.a_provider
{
    class MyType : IHoldPII { }

    bool result;
    void Because() => result = provider.CanProvide(typeof(MyType));

    [Fact] void should_be_able_to_provide() => result.ShouldBeTrue();
}
