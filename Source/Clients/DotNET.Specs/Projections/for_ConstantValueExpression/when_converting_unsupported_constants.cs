// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ConstantValueExpression;

public class when_converting_unsupported_constants : Specification
{
    readonly List<(Exception Error, Type ValueType)> _results = [];

    void Because()
    {
        foreach (var value in new object[] { new ComplexRecord("private-constant"), new ComplexClass(), new[] { "private-constant" }, new List<int> { 1 }, new CustomFormattable(), new() })
        {
            var eventBuilder = new SetBuilder<object, object, object, object>(new object(), new PropertyPath("Details.Target"), new DefaultNamingPolicy());
            var allBuilder = new AllSetBuilder<object, object>(new object(), new PropertyPath("Details.Target"), new DefaultNamingPolicy());
            _results.Add((Catch.Exception(() => eventBuilder.ToValue(value))!, value.GetType()));
            _results.Add((Catch.Exception(() => allBuilder.ToValue(value))!, value.GetType()));
        }
    }

    [Fact]
    void should_reject_at_definition_time()
    {
        foreach (var result in _results) result.Error.ShouldBeOfExactType<UnsupportedProjectionConstant>();
    }

    [Fact]
    void should_name_the_target_property()
    {
        foreach (var result in _results) result.Error.Message.ShouldContain("Details.Target");
    }

    [Fact]
    void should_name_the_value_type()
    {
        foreach (var result in _results) result.Error.Message.ShouldContain(result.ValueType.FullName!);
    }

    [Fact]
    void should_link_to_complex_constant_support()
    {
        foreach (var result in _results) result.Error.Message.ShouldContain("https://github.com/Cratis/Chronicle/issues/5525");
    }

    [Fact]
    void should_not_disclose_the_constant_value()
    {
        foreach (var result in _results) result.Error.Message.ShouldNotContain("private-constant");
    }

    record ComplexRecord(string Value);
    class ComplexClass;
    class CustomFormattable : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => "private-constant";
    }
}
