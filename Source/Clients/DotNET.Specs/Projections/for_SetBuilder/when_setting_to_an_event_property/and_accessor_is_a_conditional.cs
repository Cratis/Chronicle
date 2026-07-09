// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_property;

public class and_accessor_is_a_conditional : Specification
{
    public record SomeEvent(bool Active, string Name, string Fallback);
    public record ReadModel(string Name);

    Exception? _error;

    void Because()
    {
        var builder = new SetBuilder<ReadModel, SomeEvent, string, object>(new object(), new PropertyPath("Name"), new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.To(e => e.Active ? e.Name : e.Fallback));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
}
