// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_CompositeKeyBuilder.when_setting_a_key_part;

public class and_accessor_is_a_method_call : Specification
{
    public record KeyType(string Name);
    public record SomeEvent(string Name);

    Exception? _error;

    void Because()
    {
        var builder = new CompositeKeyBuilder<KeyType, SomeEvent>(new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Set(x => x.Name.ToUpperInvariant()));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_key_type() => _error!.Message.ShouldContain(typeof(KeyType).FullName!);
}
