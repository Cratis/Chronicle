// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ParentKeyBuilder.when_using_parent_key_from_context;

public class and_accessor_ignores_parameter : Specification
{
    public record SomeEvent(string Reference);

    Exception? _error;

    void Because()
    {
        var builder = new ParentKeyBuilder<SomeEvent, object>(new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.UsingParentKeyFromContext(_ => System.DateTimeOffset.UtcNow));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_event() => _error!.Message.ShouldContain(typeof(SomeEvent).FullName!);
}
