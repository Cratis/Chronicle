// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// A caller on the compliance surface can submit an identifier that differs only by case or trailing space from one
/// the store holds back for review. The refusal has to reach that caller as it is: not wrapped in an exception that
/// names the identifier, not swallowed, and not reported as an erasure that succeeded.
/// </summary>
public class and_the_store_refuses_the_identifier : given.a_pii_manager
{
    static readonly EncryptionKeyIdentifier _hostile = "9C1F6A3E-7D24-4B0F-8A51-6E2D3C4B5A70 ";
    Exception _error;

    void Establish()
    {
        _keyStore.AllowNewKeyFor(EventStore, EventStoreNamespace, _hostile).Returns(Task.FromException(new EncryptionKeyIdentifierUnderReview()));
        _keyStore.AllowNewKeyFor(OtherEventStore, EventStoreNamespace, _hostile).Returns(Task.FromException(new StoreUnreachable()));
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(_hostile));

    [Fact] void should_rethrow_the_refusal_unchanged() => _error.ShouldBeOfExactType<EncryptionKeyIdentifierUnderReview>();
    [Fact] void should_not_name_the_identifier_in_the_exception() => _error.ToString().ShouldNotContain(_hostile.Value.Trim(), StringComparison.OrdinalIgnoreCase);
    [Fact] void should_not_log_the_identifier() => _logger.Messages.Any(_ => _.Contains(_hostile.Value.Trim(), StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_report_success() => _logger.Entries.Any(_ => _.Level == Microsoft.Extensions.Logging.LogLevel.Information).ShouldBeFalse();
    [Fact] void should_still_evict_the_key_from_every_silo() => _cacheClient.Received(1).Evict(OtherEventStore, EventStoreNamespace, _hostile);
}
