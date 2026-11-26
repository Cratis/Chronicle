// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_an_operational_release_failure_contains_personal_data : given.a_release_with_personal_dictionary_keys
{
    async Task Because() => _result = await _manager.Release("store", "namespace", _schema, Identifier, _input);

    [Fact] void should_log_one_error() => _capturedLogger.Entries.Select(_ => _.Level).ShouldContainOnly(LogLevel.Error);
    [Fact] void should_not_count_an_unrecoverable_value() => _count.ShouldEqual(0);
    [Fact] void should_surface_the_dictionary_value_as_empty() => _result["contacts"]![DictionaryKey]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_keep_the_exception_type() => _capturedLogger.Entries.Single().State.Single(_ => _.Key == "ExceptionType").Value.ShouldEqual(nameof(InvalidOperationException));
    [Fact] void should_only_capture_the_exception_type_and_template() => _capturedLogger.Entries.Single().State.Select(_ => _.Key).ShouldContainOnly("ExceptionType", "{OriginalFormat}");
    [Fact] void should_not_log_the_identifier() => _capturedLogger.Entries.Single().Message.ShouldNotContain(Identifier);
    [Fact] void should_not_log_the_dictionary_key() => _capturedLogger.Entries.Single().Message.ShouldNotContain(DictionaryKey);
    [Fact] void should_not_include_the_identifier_in_structured_state() => _capturedLogger.Entries.Single().State.Any(_ => _.Value?.ToString()?.Contains(Identifier, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_include_the_dictionary_key_in_structured_state() => _capturedLogger.Entries.Single().State.Any(_ => _.Value?.ToString()?.Contains(DictionaryKey, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_attach_the_personal_exception() => _capturedLogger.Entries.Single().Exception.ShouldBeNull();
}
