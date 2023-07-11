// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_cloning_an_object_with_property_holding_null : Specification
{
    ExpandoObject original;
    ExpandoObject clone;
    dynamic original_dynamic;
    dynamic clone_dynamic;

    void Establish()
    {
        dynamic expando = new ExpandoObject();
        expando.Nullable = null!;
        original = expando;
    }

    void Because()
    {
        original_dynamic = original;
        clone = original.Clone();
        clone_dynamic = clone;
    }

    [Fact] void should_contain_null_for_property_in_clone() => ((object)clone_dynamic.Nullable).ShouldBeNull();
}
