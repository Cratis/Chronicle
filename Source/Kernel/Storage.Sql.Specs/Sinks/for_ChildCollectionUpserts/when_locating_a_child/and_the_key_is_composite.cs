// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionUpserts.when_locating_a_child;

public class and_the_key_is_composite : given.composite_children
{
    ExpandoObject _located;

    void Because() => _located = ChildCollectionUpserts.LocateOrCreateChild(_children, new ArrayIndexer("[children]", "id", Key()));

    [Fact] void should_locate_the_target_for_property_updates() => _located.ShouldBeSame(_target);
    [Fact] void should_not_add_a_duplicate() => _children.Count.ShouldEqual(3);
}
