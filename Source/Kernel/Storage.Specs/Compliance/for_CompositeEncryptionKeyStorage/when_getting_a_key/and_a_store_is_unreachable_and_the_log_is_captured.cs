// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

public class and_a_store_is_unreachable_and_the_log_is_captured : given.two_key_stores
{
    given.CapturingLogger<CompositeEncryptionKeyStorage> _logger;

    async Task Establish()
    {
        _logger = new();
        await Save(_secondary, KeyNamed("reachable"));
        _composite = new CompositeEncryptionKeyStorage(_logger, AnUnreachableStore(), _secondary);
    }

    async Task Because() => await _composite.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);

    [Fact] void should_log_the_failure() => _logger.Lines.ShouldNotBeEmpty();
    [Fact] void should_not_log_the_identifier() => _logger.Lines.Any(_ => _.Contains(_identifier.Value, StringComparison.Ordinal)).ShouldBeFalse();
}
