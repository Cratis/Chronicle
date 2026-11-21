// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.given;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_releasing_with_status;

public class and_an_unrecoverable_value_is_logged_after_erasure : given.a_property_handler
{
    CapturingLogger<PIICompliancePropertyValueHandler> _logger;
    JsonNode _value;
    ReleasedProtectedValue _result;

    void Establish()
    {
        _logger = new();
        _handler = new(_provisioner, _keyStore, _encryption, _logger);
        _value = JsonValue.Create(Convert.ToBase64String(new byte[256]));
        _encryption.IsEncrypted(Arg.Any<byte[]>()).Returns(true);
        _encryption.Decrypt(Arg.Any<byte[]>(), _key).Returns(_ => throw new EncryptionKeyUnwrapFailed(new CryptographicException($"Cannot unwrap {Identifier}")));
        _keyStore.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(new EncryptionKeyErasure(1, [], false));
    }

    async Task Because() => _result = await _handler.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _value);

    [Fact] void should_report_erased() => _result.Status.ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_retain_the_original_ciphertext() => _result.Value.ShouldBeSame(_value);
    [Fact] void should_log_one_warning() => _logger.Entries.Select(_ => _.Level).ShouldContainOnly(LogLevel.Warning);
    [Fact] void should_explain_why_the_value_is_treated_as_erased() => _logger.Entries.Single().Message.ShouldContain("because it cannot be unwrapped with the current key and erasure is recorded");
    [Fact] void should_record_the_exception_type() => _logger.Entries.Single().State.Single(_ => _.Key == "ExceptionType").Value.ShouldEqual(nameof(CryptographicException));
    [Fact] void should_not_name_the_subject_in_the_message() => _logger.Entries.Single().Message.ShouldNotContain(Identifier);
    [Fact] void should_not_name_the_subject_in_structured_state() => _logger.Entries.Single().State.Any(_ => _.Value?.ToString()?.Contains(Identifier, StringComparison.Ordinal) == true).ShouldBeFalse();
    [Fact] void should_not_attach_the_subject_bearing_exception() => _logger.Entries.Single().Exception.ShouldBeNull();
}
