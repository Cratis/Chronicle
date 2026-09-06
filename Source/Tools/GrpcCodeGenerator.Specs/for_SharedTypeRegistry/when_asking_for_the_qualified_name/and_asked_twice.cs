// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.SharedTypeCatalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry.when_asking_for_the_qualified_name;

/// <summary>
/// The same type is referenced from more than one service in a real run - <c>Identity</c> alone shows up across
/// most areas - and it must be discovered, and eventually generated, exactly once. A self-referential type
/// (<c>Identity.OnBehalfOf</c> is the real example) depends on the second call seeing the same cached answer
/// rather than recursing back into resolving it again.
/// </summary>
[Collection(SharedTypeRegistryCollection.Name)]
public class and_asked_twice : given.a_configured_registry
{
    string? _first;
    string? _second;

    void Because()
    {
        _first = SharedTypeRegistry.QualifiedNameFor(typeof(CoreOwnedValue));
        _second = SharedTypeRegistry.QualifiedNameFor(typeof(CoreOwnedValue));
    }

    [Fact] void should_return_the_same_name_both_times() => _second.ShouldEqual(_first);
    [Fact] void should_discover_it_only_once() =>
        SharedTypeRegistry.Discovered.Keys.Count(type => type == typeof(CoreOwnedValue)).ShouldEqual(1);
}
