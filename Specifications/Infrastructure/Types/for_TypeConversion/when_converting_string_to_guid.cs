// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_TypeConversion;

public class when_converting_string_to_guid : Specification
{
    string input;
    Guid result;

    void Establish() => input = Guid.NewGuid().ToString();

    void Because() => result = (Guid)TypeConversion.Convert(typeof(Guid), input);

    [Fact] void should_convert_correctly() => result.ToString().ShouldEqual(input);
}
