// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_EncryptionKeyCommandRunner;

public class when_the_inspection_failure_contains_sensitive_material : given.a_check
{
    const string Sentinel = "private-key-and-vault-path-sentinel-8b6a";
    ILogger _logger;
    int? _exit;

    void Establish()
    {
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _check.Inspect(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns<Task<Cratis.Chronicle.Storage.Compliance.EncryptionKeyStoreUpgradeStatus>>(_ => throw new InvalidOperationException(Sentinel));
    }

    async Task Because() => _exit = await EncryptionKeyCommandRunner.Run(_check, _logger, new EncryptionKeyCommand(true, []), _output);

    [Fact] void should_return_the_inspection_failure_code() => _exit.ShouldEqual(4);
    [Fact] void should_not_pass_an_exception_to_logging() => _logger.ReceivedCalls().SelectMany(_ => _.GetArguments()).OfType<Exception>().ShouldBeEmpty();
    [Fact] void should_not_log_sensitive_material() => _logger.ReceivedCalls().SelectMany(_ => _.GetArguments()).Any(_ => _?.ToString()?.Contains(Sentinel, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_log_a_sanitized_category() => _logger.ReceivedCalls().SelectMany(_ => _.GetArguments()).Any(_ => _?.ToString()?.Contains("InspectionFailed", StringComparison.Ordinal) == true).ShouldBeTrue();
}
