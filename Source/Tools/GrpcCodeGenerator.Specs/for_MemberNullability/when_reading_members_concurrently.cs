// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_MemberNullability;

public class when_reading_members_concurrently : Specification
{
    const int Reads = 4_000;

    PropertyInfo[] _properties;
    ParameterInfo[] _parameters;
    ConcurrentBag<string> _misreadProperties;
    ConcurrentBag<string> _misreadParameters;

    void Establish()
    {
        _properties = typeof(Members).GetProperties();
        _parameters = typeof(Members).GetConstructors().Single().GetParameters();
        _misreadProperties = [];
        _misreadParameters = [];
    }

    void Because() => Parallel.For(0, Reads, read =>
    {
        var property = _properties[read % _properties.Length];
        if (MemberNullability.Of(property) != property.Name.StartsWith("Optional", StringComparison.Ordinal))
        {
            _misreadProperties.Add(property.Name);
        }

        var parameter = _parameters[read % _parameters.Length];
        if (MemberNullability.Of(parameter) != parameter.Name!.StartsWith("Optional", StringComparison.Ordinal))
        {
            _misreadParameters.Add(parameter.Name);
        }
    });

    [Fact] void should_read_every_property_as_declared() => _misreadProperties.ShouldBeEmpty();
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
        int? OptionalSixth,
        int RequiredSixth,
        Guid? OptionalSeventh,
        Guid RequiredSeventh,
        string[]? OptionalEighth,
        string[] RequiredEighth);
}
