// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ChildrenBuilder.when_identifying_children;

public class and_accessor_is_member_access : Specification
{
    public record Parent(string Name);
    public record Child(string Id);

    Exception? _error;

    void Because()
    {
        var builder = new ChildrenBuilder<Parent, Child>(new DefaultNamingPolicy(), Substitute.For<IEventTypes>(), new JsonSerializerOptions(), AutoMap.Inherit);
        _error = Catch.Exception(() => builder.IdentifiedBy(c => c.Id));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
