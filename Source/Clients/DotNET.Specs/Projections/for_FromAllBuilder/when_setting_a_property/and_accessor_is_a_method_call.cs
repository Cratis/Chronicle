// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_FromAllBuilder.when_setting_a_property;

public class and_accessor_is_a_method_call : Specification
{
    public record ReadModel(string Name);

    Exception? _error;

    void Because()
    {
        var builder = new FromAllBuilder<ReadModel>(new DefaultNamingPolicy());
        _error = Catch.Exception(() => builder.Set(m => m.Name.ToUpperInvariant()));
    }

    [Fact] void should_throw_invalid_property_expression() => _error.ShouldBeOfExactType<InvalidPropertyExpression>();
    [Fact] void should_name_the_read_model() => _error!.Message.ShouldContain(typeof(ReadModel).FullName!);
}
