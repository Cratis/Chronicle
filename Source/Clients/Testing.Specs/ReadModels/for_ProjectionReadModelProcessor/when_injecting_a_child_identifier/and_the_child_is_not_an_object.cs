// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Testing.ReadModels.for_ProjectionReadModelProcessor.when_injecting_a_child_identifier;

public class and_the_child_is_not_an_object : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ProjectionReadModelProcessor.InjectChildIdentifierFromKey(
        new ChildAdded("child", new PropertyPath("items"), new PropertyPath("id"), 42, ArrayIndexers.NoIndexers)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
