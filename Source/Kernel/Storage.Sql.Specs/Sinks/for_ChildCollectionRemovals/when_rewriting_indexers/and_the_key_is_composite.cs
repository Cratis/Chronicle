// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionRemovals.when_rewriting_indexers;

public class and_the_key_is_composite : given.composite_children
{
    ExpandoObject _key;
    ArrayIndexer[] _rewritten;

    void Establish() => _key = Key();

    void Because() => _rewritten = ChildCollectionRemovals.RewriteIndexersRelativeTo("parents", new ArrayIndexers([
        new ArrayIndexer("[parents].[children]", "id", _key),
        new ArrayIndexer("[parents].[other]", "id", 7L)
    ])).All.ToArray();

    [Fact] void should_keep_the_composite_identity_intact() => _rewritten[0].Identifier.ShouldBeSame(_key);
    [Fact] void should_make_the_path_relative_to_the_column() => _rewritten[0].ArrayProperty.Path.ShouldEqual("[children]");
    [Fact] void should_keep_scalar_string_normalization() => _rewritten[1].Identifier.ShouldEqual("7");
}
