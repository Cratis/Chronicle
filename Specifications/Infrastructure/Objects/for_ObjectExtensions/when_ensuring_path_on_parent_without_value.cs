// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Objects.for_ObjectExtensions;

public class when_ensuring_path_on_parent_without_value : Specification
{
    TopLevel instance;
    PropertyPath path;
    FirstLevel result;

    void Establish()
    {
        instance = new TopLevel(null);
        path = new($"{nameof(TopLevel.FirstLevel)}.{nameof(FirstLevel.SomeProperty)}");
    }

    void Because() => result = instance.EnsurePath(path, ArrayIndexers.NoIndexers) as FirstLevel;

    [Fact] void should_return_the_instance() => result.ShouldNotBeNull();
}
