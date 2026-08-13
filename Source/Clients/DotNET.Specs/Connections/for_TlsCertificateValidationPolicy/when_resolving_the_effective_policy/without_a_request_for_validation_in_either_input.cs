// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.for_TlsCertificateValidationPolicy.when_resolving_the_effective_policy;

/// <summary>
/// The out-of-the-box case: a development client connecting to the server's generated self-signed
/// certificate, which is never persisted or trusted and so cannot be validated against anything.
/// </summary>
public class without_a_request_for_validation_in_either_input : Specification
{
    bool _result;

    void Because() => _result = TlsCertificateValidationPolicy.ShouldSkip(
        new Tls(),
        new ChronicleConnectionString("chronicle://localhost:35000"));

    [Fact] void should_skip_validation() => _result.ShouldBeTrue();
}
