// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_AddBuilder.when_adding_with;

public class and_accessor_is_computed : Specification
{
    public record SomeEvent(int First, int Second, int Amount);
    public record ReadModel(int Total);

    Exception? _error;

    void Because()
    {
        var builder = new AddBuilder<ReadModel, SomeEvent, int, IFromBuilder<ReadModel, SomeEvent>>(Substitute.For<IFromBuilder<ReadModel, SomeEvent>>(), new PropertyPath("Total"), new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.With(e => e.First + e.Second));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_target_property() => _error!.Message.ShouldContain("Total");
    [Fact] void should_name_the_read_model() => _error!.Message.ShouldContain(typeof(ReadModel).FullName!);
}
