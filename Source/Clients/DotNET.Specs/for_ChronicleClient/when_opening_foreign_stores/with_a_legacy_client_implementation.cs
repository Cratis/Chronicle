// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class with_a_legacy_client_implementation : Specification
{
    IChronicleClient _client;
    IEventStore _expected;
    IEventStore _legacyStore;
    Exception _exception;

    void Establish()
    {
        _expected = Substitute.For<IEventStore>();
        _client = new LegacyClient(_expected);
    }

    async Task Because()
    {
        _legacyStore = await _client.GetEventStore("own");
        _exception = await Catch.Exception(() => _client.GetEventStore("foreign", null, RegistrationMode.EventTypesOnly));
    }

    [Fact] void should_keep_the_original_overload_working() => _legacyStore.ShouldBeSame(_expected);
    [Fact] void should_reject_an_unsupported_registration_mode() => _exception.ShouldBeOfExactType<NotSupportedException>();
    [Fact] void should_explain_the_unsupported_capability() => _exception.Message.ShouldEqual("Per-store registration modes are not supported by this client.");

    sealed class LegacyClient(IEventStore store) : IChronicleClient
    {
        public ChronicleOptions Options { get; } = new();
        public ICausationManager CausationManager { get; } = Substitute.For<ICausationManager>();

        public Task<IEventStore> GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = default) => Task.FromResult(store);

        public Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<EventStoreName>>([]);

        public void EvictEventStores()
        {
        }
    }
}
