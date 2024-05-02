// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Types.for_TypeConversion;

public class when_converting_to_same_type : Specification
{
    Guid input;
    Guid result;

    void Establish() => input = Guid.NewGuid();

    void Because() => result = (Guid)TypeConversion.Convert(typeof(Guid), input);

    [Fact] void should_convert_correctly() => result.ShouldEqual(input);
}
