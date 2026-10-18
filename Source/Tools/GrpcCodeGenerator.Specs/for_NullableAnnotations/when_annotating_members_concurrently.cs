// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_NullableAnnotations;

public class when_annotating_members_concurrently : Specification
{
    const int Reads = 4_000;

    PropertyInfo[] _properties;
    ParameterInfo[] _parameters;
    ConcurrentBag<string> _misannotatedProperties;
    ConcurrentBag<string> _misannotatedParameters;
    ConcurrentBag<string> _misreadParameters;

    void Establish()
    {
        _properties = typeof(Members).GetProperties();
        _parameters = typeof(Members).GetConstructors().Single().GetParameters();
        _misannotatedProperties = [];
        _misannotatedParameters = [];
        _misreadParameters = [];
    }

    void Because() => Parallel.For(0, Reads, read =>
    {
        var property = _properties[read % _properties.Length];
        var propertyIsOptional = property.Name.StartsWith("Optional", StringComparison.Ordinal);
        if (NullableAnnotations.For("TypeName", property) != (propertyIsOptional ? "TypeName?" : "TypeName"))
        {
            _misannotatedProperties.Add(property.Name);
        }

        var parameter = _parameters[read % _parameters.Length];
        var parameterIsOptional = parameter.Name!.StartsWith("Optional", StringComparison.Ordinal);
        if (NullableAnnotations.For("TypeName", parameter) != (parameterIsOptional ? "TypeName?" : "TypeName"))
        {
            _misannotatedParameters.Add(parameter.Name);
        }

        if (NullableAnnotations.IsNullable(parameter) != parameterIsOptional)
        {
            _misreadParameters.Add(parameter.Name);
        }
    });

    [Fact] void should_annotate_every_property_as_declared() => _misannotatedProperties.ShouldBeEmpty();
    [Fact] void should_annotate_every_parameter_as_declared() => _misannotatedParameters.ShouldBeEmpty();
    [Fact] void should_read_every_parameter_as_declared() => _misreadParameters.ShouldBeEmpty();

    sealed record Members(
        string? OptionalFirst,
        string RequiredFirst,
        object? OptionalSecond,
        object RequiredSecond,
        Uri? OptionalThird,
        Uri RequiredThird,
        Version? OptionalFourth,
        Version RequiredFourth,
        IEnumerable<string>? OptionalFifth,
        IEnumerable<string> RequiredFifth,
        string[]? OptionalSixth,
        string[] RequiredSixth);
}
