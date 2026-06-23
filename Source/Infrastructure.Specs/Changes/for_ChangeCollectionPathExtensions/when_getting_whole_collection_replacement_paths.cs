// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_ChangeCollectionPathExtensions;

public class when_getting_whole_collection_replacement_paths : Specification
{
    ISet<PropertyPath> _result;

    void Because()
    {
        var changes = new Change[]
        {
            new PropertiesChanged<ExpandoObject>(
                new ExpandoObject(),
                [
                    new PropertyDifference("[contacts]", null, Array.Empty<object>()),
                    new PropertyDifference("status", "pending", "approved")
                ])
        };

        _result = changes.GetWholeCollectionReplacementPaths();
    }

    [Fact] void should_include_the_whole_collection_path() => _result.Contains(new PropertyPath("contacts")).ShouldBeTrue();

    [Fact] void should_not_include_a_scalar_property_path() => _result.Contains(new PropertyPath("status")).ShouldBeFalse();
}
