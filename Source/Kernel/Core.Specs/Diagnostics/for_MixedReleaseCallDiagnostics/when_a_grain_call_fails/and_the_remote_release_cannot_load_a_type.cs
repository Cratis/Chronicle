// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.when_a_grain_call_fails;

public class and_the_remote_release_cannot_load_a_type : given.a_call_filter
{
    Task Because() => Invoke();
    [Fact] void should_rethrow_the_same_exception() => _failure.ShouldEqual(_original);
    [Fact] void should_log_the_interface_and_method() => _messages.Single().ShouldContain("IPatchManager.ApplyPatchesFor");
    [Fact] void should_name_the_exception_type() => _messages.Single().ShouldContain("TypeLoadException");
    [Fact] void should_include_the_cluster_release_summary() => _messages.Single().ShouldContain("19.111.0: 1");
    [Fact] void should_not_log_exception_text() => _messages.Single().ShouldNotContain("hostile-exception-content");
    [Fact] void should_not_pass_the_exception_to_the_logger() => _loggedExceptions.Single().ShouldBeNull();
}
