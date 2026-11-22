// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_incarnations_are_enabled_for_a_cached_namespace : given.a_namespaces_grain
{
    Exception _error;

    void Establish()
    {
        _state.Namespaces.Add(new NamespaceState(_namespace, DateTimeOffset.UtcNow));
        _incarnations.Enabled.Returns(true);
        _incarnations.Ensure(_eventStore, _namespace).Returns(Task.FromException<EventStoreNamespaceName>(new NamespaceClosing(_namespace)));
    }

    async Task Because() => _error = await Catch.Exception(() => _namespaces.EnsureCanonical(_namespace));

    [Fact] void should_not_admit_a_cached_closing_name() => _error.ShouldBeOfExactType<NamespaceClosing>();
    [Fact] async Task should_consult_the_owner_even_for_an_existing_name() => await _incarnations.Received(1).Ensure(_eventStore, _namespace);
    [Fact] void should_not_enqueue_legacy_creation() => _state.NewNamespaces.Count.ShouldEqual(0);
}
