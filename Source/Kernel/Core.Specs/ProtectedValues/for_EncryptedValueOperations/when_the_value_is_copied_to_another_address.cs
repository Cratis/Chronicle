// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueOperations;

public class when_the_value_is_copied_to_another_address : given.a_signed_confidentiality_value
{
    Task Because() => ReleaseAt(_address with { Identifier = "another-subject" });

    [Fact] void should_refuse_strict_release_even_when_the_same_key_is_available() => _strictFailure.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
    [Fact] void should_not_release_plaintext() => _strictValue.ShouldBeNull();
    [Fact] void should_refuse_status_release() => _statusFailure.ShouldBeOfExactType<EncryptionEnvelopeBindingFailed>();
    [Fact] void should_not_return_a_status_result() => _statusValue.ShouldBeNull();
    [Fact] void should_preserve_lenient_release() => _lenientValue.ToString().ShouldEqual("value");
}
