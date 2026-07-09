// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_SetBuilder.when_setting_to_an_event_property;

public class and_accessor_is_computed : Specification
{
    public record SomeEvent(string FirstName, string LastName);
    public record ReadModel(string FullName);

    Exception? _error;

    void Because()
    {
        var builder = new SetBuilder<ReadModel, SomeEvent, string, object>(new object(), new PropertyPath("FullName"), new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.To(e => $"{e.FirstName} {e.LastName}"));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_target_property() => _error!.Message.ShouldContain("FullName");
    [Fact] void should_name_the_read_model() => _error!.Message.ShouldContain(typeof(ReadModel).FullName!);
}
