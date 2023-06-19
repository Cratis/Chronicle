// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_type;

public class and_there_is_no_metadata : given.a_provider
{
    bool result;
    void Because() => result = provider.CanProvide(typeof(object));

    [Fact] void should_not_be_able_to_provide() => result.ShouldBeFalse();
}
