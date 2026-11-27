// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_Namespaces;

public class when_a_stale_incarnation_callback_arrives : given.a_namespaces_grain
{
    readonly NamespaceIncarnationId _identity = NamespaceIncarnationId.New();
    readonly Guid _operation = Guid.NewGuid();
    Exception _error;

    void Establish()
    {
        _state.Namespaces.Add(new NamespaceState(_namespace, DateTimeOffset.UtcNow));
        _incarnations.Enabled.Returns(true);
        _incarnations.ValidateForget(_eventStore, _namespace, _identity, _operation).Returns(Task.FromException(new NamespaceLifecycleChanged(_namespace)));
    }

    async Task Because() => _error = await Catch.Exception(() => _namespaces.ForgetIncarnation(_namespace, _identity, _operation));

    [Fact] void should_refuse_the_callback() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
    [Fact] void should_keep_the_listing() => _state.Namespaces.Count.ShouldEqual(1);
}
