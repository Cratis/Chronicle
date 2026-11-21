// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ProtectedValues.for_EncryptedValueOperations;

public class when_releasing_at_the_original_address : given.a_signed_confidentiality_value
{
    Task Because() => ReleaseAt(_address);

    [Fact] void should_allow_strict_release() => _strictFailure.ShouldBeNull();
    [Fact] void should_return_the_plaintext() => _strictValue.ToString().ShouldEqual("value");
    [Fact] void should_allow_status_release() => _statusFailure.ShouldBeNull();
    [Fact] void should_report_decrypted() => _statusValue.Status.ShouldEqual(ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_return_the_plaintext_with_status() => _statusValue.Value.ToString().ShouldEqual("value");
    [Fact] void should_preserve_lenient_release() => _lenientValue.ToString().ShouldEqual("value");
}
