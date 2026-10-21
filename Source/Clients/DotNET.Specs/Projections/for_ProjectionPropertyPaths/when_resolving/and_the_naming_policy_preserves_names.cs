// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ProjectionPropertyPaths.when_resolving;

public class and_the_naming_policy_preserves_names : Specification
{
    PropertyPath _result;

    void Because() => _result = ProjectionPropertyPaths.Resolve("Brief.Topic", new DefaultNamingPolicy(), ProjectionPathRole.Target);

    [Fact] void should_preserve_every_segment() => _result.Path.ShouldEqual("Brief.Topic");
}
