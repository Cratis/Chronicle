// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object;

public class and_accessor_is_member_access : Specification
{
    public record Parent(string Name);
    public record Child();
    public record SomeEvent(Child TheChild);

    Exception? _error;

    void Because()
    {
        var childrenBuilder = Substitute.For<IChildrenBuilder<Parent, Child>>();
        var fromBuilder = Substitute.For<IFromBuilder<Child, SomeEvent>>();
        var builder = new AddChildBuilder<Parent, Child, SomeEvent>(childrenBuilder, fromBuilder, new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.FromObject(e => e.TheChild));
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
