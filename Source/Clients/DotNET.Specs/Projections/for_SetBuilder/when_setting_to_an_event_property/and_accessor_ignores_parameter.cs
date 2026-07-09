// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_property;

public class and_accessor_ignores_parameter : Specification
{
    public record SomeEvent(string Name);
    public record ReadModel(System.DateTimeOffset When);

    Exception? _error;

    void Because()
    {
        var builder = new SetBuilder<ReadModel, SomeEvent, System.DateTimeOffset, object>(new object(), new PropertyPath("When"), new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.To(_ => System.DateTimeOffset.UtcNow));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
}
