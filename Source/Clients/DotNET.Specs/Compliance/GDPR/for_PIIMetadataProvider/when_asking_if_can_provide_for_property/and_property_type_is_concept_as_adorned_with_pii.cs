// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIMetadataProvider.when_asking_if_can_provide_for_property;

public class and_property_type_is_concept_as_adorned_with_pii : given.a_provider
{
    [PII]
    record PersonName(string Value) : ConceptAs<string>(Value);

    class MyEvent
    {
        public PersonName Name { get; set; }

        public static readonly PropertyInfo NameProperty = typeof(MyEvent).GetProperty(nameof(Name), BindingFlags.Public | BindingFlags.Instance)!;
    }

    bool _result;

    void Because() => _result = provider.CanProvide(MyEvent.NameProperty);

    [Fact] void should_be_able_to_provide() => _result.ShouldBeTrue();
}
