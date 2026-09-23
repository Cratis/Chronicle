// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Confidentiality;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedMetadataProvider.when_providing_for_property;

public class and_there_is_metadata_and_details : given.a_provider
{
    const string Details = "These are the details";

    [Encrypted(details: Details)]
    record ApiKey(string Value) : ConceptAs<string>(Value);

    class MyClass
    {
        public ApiKey Key { get; set; }

        public static readonly PropertyInfo KeyProperty = typeof(MyClass).GetProperty(nameof(Key), BindingFlags.Public | BindingFlags.Instance);
    }

    SecurityMetadata _result;

    void Because() => _result = provider.Provide(MyClass.KeyProperty);

    [Fact] void should_return_encrypted_subject_metadata() => _result.MetadataType.ShouldEqual(SecurityMetadataType.EncryptedSubject);
    [Fact] void should_return_metadata_with_details() => _result.Details.ShouldEqual(Details);
}
