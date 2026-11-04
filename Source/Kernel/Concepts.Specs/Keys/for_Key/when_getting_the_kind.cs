// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Keys.for_Key;

public class when_getting_the_kind : Specification
{
    IDictionary<string, object?> _composite;

    void Establish()
    {
        _composite = new ExpandoObject();
        _composite["first"] = "abc";
        _composite["second"] = 42;
    }

    [Fact] void should_classify_a_string() => new Key("abc", ArrayIndexers.NoIndexers).Kind.ShouldEqual(KeyKind.String);
    [Fact] void should_classify_a_guid() => new Key(Guid.NewGuid(), ArrayIndexers.NoIndexers).Kind.ShouldEqual(KeyKind.Guid);
    [Fact] void should_classify_an_int() => new Key(42, ArrayIndexers.NoIndexers).Kind.ShouldEqual(KeyKind.Int);
    [Fact] void should_classify_a_long() => new Key(42L, ArrayIndexers.NoIndexers).Kind.ShouldEqual(KeyKind.Long);
    [Fact] void should_classify_a_composite() => new Key(_composite, ArrayIndexers.NoIndexers).Kind.ShouldEqual(KeyKind.Composite);
    [Fact] void should_classify_an_undefined_key_as_string() => Key.Undefined.Kind.ShouldEqual(KeyKind.String);
    [Fact] void should_classify_an_unlisted_scalar_as_string() => new Key(42.5, ArrayIndexers.NoIndexers).Kind.ShouldEqual(KeyKind.String);
}
