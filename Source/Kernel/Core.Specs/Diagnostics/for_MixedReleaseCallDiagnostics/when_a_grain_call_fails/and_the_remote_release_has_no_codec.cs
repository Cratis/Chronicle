// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.when_a_grain_call_fails;

public class and_the_remote_release_has_no_codec : given.a_call_filter
{
    void Establish() => _original = new CodecNotFoundException("payload-name");
    Task Because() => Invoke();
    [Fact] void should_classify_the_missing_codec() => _messages.Single().ShouldContain("CodecNotFoundException");
    [Fact] void should_preserve_the_exception() => _failure.ShouldEqual(_original);
}
