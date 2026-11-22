// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_startup_fails_before_the_active_hold : given.a_startup_diagnostic
{
    void Establish()
    {
        Startup = Task.FromException(new InvalidOperationException("Password=diagnostic-secret;Server=private-host", new SocketException((int)SocketError.AddressAlreadyInUse)));
        foreach (var index in Enumerable.Range(0, 80)) Hold.Diagnostics.Record($"marker-{index}");
    }

    Task Because() => CaptureTimeout();

    [Fact] void should_preserve_the_wait_timeout() => Error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_capture_faulted_startup() => Message.ShouldContain("hostStart=Faulted");
    [Fact] void should_capture_the_startup_exception_type() => Message.ShouldContain(typeof(InvalidOperationException).FullName);
    [Fact] void should_capture_the_inner_socket_exception() => Message.ShouldContain(typeof(SocketException).FullName);
    [Fact] void should_capture_the_socket_error_code() => Message.ShouldContain("SocketError=AddressAlreadyInUse");
    [Fact] void should_capture_exception_error_codes() => Message.ShouldContain("HResult=0x");
    [Fact] void should_not_capture_exception_messages() => Message.ShouldNotContain("diagnostic-secret");
    [Fact] void should_not_capture_connection_strings() => Message.ShouldNotContain("Server=private-host");
    [Fact] void should_distinguish_not_reaching_the_lifecycle() => Message.ShouldContain("lastLifecycleStage=none observed");
    [Fact] void should_report_truncated_steps() => Message.ShouldContain("omittedSteps=20");
    [Fact] void should_bound_the_number_of_steps() => Message.ShouldNotContain("marker-79");
    [Fact] void should_bound_the_output_size() => (Message.Length < 20000).ShouldBeTrue();
}
