// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.when_releasing_strictly;

public class when_an_unexpected_failure_occurs : given.a_compliance_service
{
    ILogger _logger;
    object?[] _logArguments;
    Dictionary<string, object?> _properties;

    void Establish()
    {
        var manager = Substitute.For<IJsonSchemaMetadataManager>();
        manager.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns<Task<JsonObject>>(_ => throw new InvalidOperationException($"{SensitiveValue}: {request.Payload}"));
        _logger = Substitute.For<ILogger>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        service = new ComplianceService(Substitute.For<IGrainFactory>(), manager, new Logger<ComplianceService>(loggerFactory));
    }

    async Task Because()
    {
        result = await service.Release(request);
        _logArguments = _logger.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(ILogger.Log)).GetArguments();
        _properties = ((IEnumerable<KeyValuePair<string, object?>>)_logArguments[2]!).ToDictionary(_ => _.Key, _ => _.Value);
    }

    [Fact] void should_fail_the_release() => result.HasError.ShouldBeTrue();
    [Fact] void should_return_no_payload() => result.Payload.ShouldBeEmpty();
    [Fact] void should_report_strict_mode() => result.Strict.ShouldBeTrue();
    [Fact] void should_have_no_known_failed_paths() => result.FailedProperties.ShouldBeEmpty();
    [Fact] void should_return_only_the_safe_error() => result.Error.ShouldEqual("Protected content could not be released.");
    [Fact] void should_log_at_warning() => _logArguments[0].ShouldEqual(LogLevel.Warning);
    [Fact] void should_log_the_exception_type() => _properties["ExceptionType"].ShouldEqual(nameof(InvalidOperationException));
    [Fact] void should_log_an_empty_path_when_unknown() => _properties["PropertyPath"].ShouldEqual(string.Empty);
    [Fact] void should_log_only_type_and_path_properties() => _properties.Keys.ShouldContainOnly("ExceptionType", "PropertyPath", "{OriginalFormat}");
    [Fact] void should_not_attach_the_exception() => _logArguments[3].ShouldBeNull();
    [Fact] void should_not_log_values_or_payload() => _logArguments[2]!.ToString().ShouldEqual("Failed to strictly release compliance with exception type 'InvalidOperationException' for property ''");
}
