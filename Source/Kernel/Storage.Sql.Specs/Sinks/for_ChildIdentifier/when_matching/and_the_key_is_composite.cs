// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildIdentifier.when_matching;

public class and_the_key_is_composite : given.composite_children
{
    int _eagerIndex;
    int _lazyIndex;
    IDictionary<string, object?>? _document;

    void Because()
    {
        var identifier = ChildIdentifier.For("id", Key());
        _eagerIndex = identifier.FindIndex(_children, true);
        _lazyIndex = identifier.FindIndex(_children, false);
        _document = identifier.FindDocument(_children);
    }

    [Fact] void should_find_the_target_eagerly() => _eagerIndex.ShouldEqual(1);
    [Fact] void should_find_the_target_lazily() => _lazyIndex.ShouldEqual(1);
    [Fact] void should_find_the_target_document() => _document.ShouldBeSame(_target);
}
