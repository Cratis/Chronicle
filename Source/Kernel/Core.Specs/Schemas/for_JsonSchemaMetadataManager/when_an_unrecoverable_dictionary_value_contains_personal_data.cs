// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_an_unrecoverable_dictionary_value_contains_personal_data : given.a_release_with_personal_dictionary_keys
{
    void Establish() => _error = new EncryptionKeyUnrecoverable(3, _error);

    async Task Because() => _result = await _manager.Release("store", "namespace", _schema, Identifier, _input);

    [Fact] void should_log_one_error() => _capturedLogger.Entries.Select(_ => _.Level).ShouldContainOnly(LogLevel.Error);
    [Fact] void should_count_the_unrecoverable_value() => _count.ShouldEqual(1);
    [Fact] void should_surface_the_dictionary_value_as_empty() => _result["contacts"]![DictionaryKey]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_keep_the_store_and_namespace_context() => _capturedLogger.Entries.Single().Message.ShouldContain("store/namespace");
    [Fact] void should_keep_the_action_context() => _capturedLogger.Entries.Single().Message.ShouldContain("during release");
    [Fact] void should_keep_the_exception_type() => _capturedLogger.Entries.Single().State.Single(_ => _.Key == "ExceptionType").Value.ShouldEqual(nameof(InvalidOperationException));
    [Fact] void should_keep_the_attempted_revisions() => _capturedLogger.Entries.Single().State.Single(_ => _.Key == "AttemptedRevisions").Value.ShouldEqual(3);
    [Fact] void should_not_log_the_identifier() => _capturedLogger.Entries.Single().Message.ShouldNotContain(Identifier);
    [Fact] void should_not_log_the_dictionary_key() => _capturedLogger.Entries.Single().Message.ShouldNotContain(DictionaryKey);
    [Fact] void should_not_include_the_identifier_in_structured_state() => _capturedLogger.Entries.Single().State.Any(_ => _.Value?.ToString()?.Contains(Identifier, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_include_the_dictionary_key_in_structured_state() => _capturedLogger.Entries.Single().State.Any(_ => _.Value?.ToString()?.Contains(DictionaryKey, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_attach_the_personal_exception() => _capturedLogger.Entries.Single().Exception.ShouldBeNull();
}
