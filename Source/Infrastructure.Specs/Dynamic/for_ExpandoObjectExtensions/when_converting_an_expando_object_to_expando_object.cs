// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_converting_an_expando_object_to_expando_object : Specification
{
    ExpandoObject _original;
    ExpandoObject _result;

    void Establish() => _original = new ExpandoObject();

    void Because() => _result = _original.AsExpandoObject(camelCaseProperties: true);

    [Fact] void should_return_the_same_instance() => ReferenceEquals(_result, _original).ShouldBeTrue();
}
