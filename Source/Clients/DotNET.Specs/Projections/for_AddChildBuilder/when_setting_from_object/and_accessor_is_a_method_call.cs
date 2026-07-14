// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_AddChildBuilder.when_setting_from_object;

public class and_accessor_is_a_method_call : Specification
{
    public record Parent(string Name);
    public record Child();
    public record SomeEvent(Child TheChild);

    Exception? _error;

    static Child Identity(Child child) => child;

    void Because()
    {
        var childrenBuilder = Substitute.For<IChildrenBuilder<Parent, Child>>();
        var fromBuilder = Substitute.For<IFromBuilder<Child, SomeEvent>>();
        var builder = new AddChildBuilder<Parent, Child, SomeEvent>(childrenBuilder, fromBuilder, new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.FromObject(e => Identity(e.TheChild)));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_event() => _error!.Message.ShouldContain(typeof(SomeEvent).FullName!);
}
