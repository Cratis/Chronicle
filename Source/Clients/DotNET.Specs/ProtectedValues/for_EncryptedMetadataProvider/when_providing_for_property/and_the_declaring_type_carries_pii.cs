// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_providing_for_property;

public class and_the_declaring_type_carries_pii : given.a_provider
{
    [PII]
    class MyClass
    {
        [Encrypted]
        public string Something { get; set; }

        public static readonly PropertyInfo SomethingProperty = typeof(MyClass).GetProperty(nameof(Something), BindingFlags.Public | BindingFlags.Instance);
    }

    Exception _result;

    void Because() => _result = Catch.Exception(() => provider.Provide(MyClass.SomethingProperty));

    [Fact] void should_throw_pii_and_encrypted_combined_not_supported() => _result.ShouldBeOfExactType<PIIAndEncryptedCombinedNotSupported>();
}
