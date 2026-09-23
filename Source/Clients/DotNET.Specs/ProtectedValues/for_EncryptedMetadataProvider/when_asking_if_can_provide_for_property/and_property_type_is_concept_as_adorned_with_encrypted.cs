// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_asking_if_can_provide_for_property;

public class and_property_type_is_concept_as_adorned_with_encrypted : given.a_provider
{
    [Encrypted]
    record ApiKey(string Value) : ConceptAs<string>(Value);

    class MyEvent
    {
        public ApiKey Key { get; set; }

        public static readonly PropertyInfo KeyProperty = typeof(MyEvent).GetProperty(nameof(Key), BindingFlags.Public | BindingFlags.Instance);
    }

    bool _result;

    void Because() => _result = provider.CanProvide(MyEvent.KeyProperty);

    [Fact] void should_be_able_to_provide() => _result.ShouldBeTrue();
}
