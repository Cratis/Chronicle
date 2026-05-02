// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_property;

public class and_constructor_parameter_is_adorned_with_pii_attribute : given.a_provider
{
    record MyRecord([PII] string Name, string Other);

    bool _result;

    void Because() => _result = provider.CanProvide(
        typeof(MyRecord).GetProperty(nameof(MyRecord.Name), BindingFlags.Public | BindingFlags.Instance)!);

    [Fact] void should_be_able_to_provide() => _result.ShouldBeTrue();
}
