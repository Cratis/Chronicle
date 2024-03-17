// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_TypeConversion;

public class when_converting_guid_concept_to_string : Specification
{
    GuidConcept input;
    string result;

    void Establish() => input = Guid.NewGuid();

    void Because() => result = TypeConversion.Convert(typeof(string), input) as string;

    [Fact] void should_convert_correctly() => result.ShouldEqual(input.ToString());
}
