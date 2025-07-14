// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_type;

public class and_type_is_adorned_with_pii_attribute : given.a_provider
{
    [PII]
    class MyType;

    bool _result;

    void Because() => _result = provider.CanProvide(typeof(MyType));

    [Fact] void should_be_able_to_provide() => _result.ShouldBeTrue();
}
