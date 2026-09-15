// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternScope.when_getting_all_pattern_scopes;

public class a_scope_with_no_matching_identity : Specification
{
    const string EventStore = "some-store";
    const string Subject = "user-42";

    IStorage _storage;
    IEnumerable<PatternScope> _result;

    void Establish()
    {
        var patterns = Substitute.For<IBehaviorPatternStorage>();
        patterns.GetScopes().Returns([new PatternGroupingKey(Subject)]);

        var identities = Substitute.For<IIdentityStorage>();
        identities.GetAll().Returns([]);

        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _storage.GetEventStore(new EventStoreName(EventStore)).Returns(eventStore);
        eventStore.GetNamespace(EventStoreNamespaceName.Default).Returns(@namespace);
        @namespace.Patterns.Returns(patterns);
        @namespace.Identities.Returns(identities);
    }

    async Task Because() => _result = await PatternScope.AllPatternScopes(EventStore, EventStoreNamespaceName.Default, _storage);

    [Fact] void should_fall_back_to_the_id_as_the_name() => _result.Single().Name.ShouldEqual(Subject);
    [Fact] void should_fall_back_to_the_id_as_the_username() => _result.Single().UserName.ShouldEqual(Subject);
}
