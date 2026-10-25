// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_KernelFailureGuard;

public class when_exit_diagnostics_contain_sensitive_or_excessive_output : Specification
{
    Exception? _failure;

    async Task Because()
    {
        var tail = new string('x', 20000) + "\n" + string.Join('\n', Enumerable.Repeat("ordinary log line", 70))
            + "\nPassword=very-private\nAuthorization: bearer private-value\nhttps://user:private@host\nlast\u001bline"
            + "\nENVIRONMENT:\n{\n  arbitrary_name: [\n    'unlabelled-private-value'\n  ]\n}\n-----BEGIN PRIVATE KEY-----\nraw-key-material\n-----END PRIVATE KEY-----"
            + "\nHost=database;Database=prod;Username=someone\nSystem.InvalidOperationException: private-message\nOut of memory.";
        var guard = new KernelFailureGuard(
            "kernel",
            _ => Task.FromResult<long?>(137),
            _ => Task.FromResult(tail),
            _ => Task.CompletedTask);
        _failure = await Catch.Exception(() => guard.Run(_ => Task.CompletedTask, () => true));
    }

    [Fact] void should_bound_the_number_of_log_lines() => _failure!.Message.Split('\n').Length.ShouldEqual(64);
    [Fact] void should_bound_the_log_size() => (_failure!.Message.Length < 16500).ShouldBeTrue();
    [Fact] void should_redact_passwords() => _failure!.Message.ShouldNotContain("very-private");
    [Fact] void should_redact_authorization() => _failure!.Message.ShouldNotContain("private-value");
    [Fact] void should_redact_uri_credentials() => _failure!.Message.ShouldNotContain("user:private");
    [Fact] void should_not_include_raw_environment() => _failure!.Message.ShouldNotContain("arbitrary_name");
    [Fact] void should_redact_unlabelled_multiline_values() => _failure!.Message.ShouldNotContain("unlabelled-private-value");
    [Fact] void should_redact_multiline_key_material() => _failure!.Message.ShouldNotContain("raw-key-material");
    [Fact] void should_redact_connection_strings_without_passwords() => _failure!.Message.ShouldNotContain("Host=database");
    [Fact] void should_redact_arbitrary_exception_messages() => _failure!.Message.ShouldNotContain("private-message");
    [Fact] void should_strip_terminal_controls() => _failure!.Message.ShouldNotContain("\u001b", StringComparison.Ordinal);
    [Fact] void should_retain_a_fixed_exception_marker() => _failure!.Message.ShouldContain("System.InvalidOperationException");
    [Fact] void should_retain_the_crash_marker() => _failure!.Message.ShouldContain("Out of memory.");
}
