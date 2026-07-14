// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_KeyAndParentKeyBuilder.when_using_parent_key;

public class and_accessor_is_a_method_call : Specification
{
    public record SomeEvent(string Reference);

    Exception? _error;

    void Because()
    {
        var builder = new KeyAndParentKeyBuilder<SomeEvent, object>(new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.UsingParentKey(e => e.Reference.ToUpperInvariant()));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_event() => _error!.Message.ShouldContain(typeof(SomeEvent).FullName!);
}
