// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Objects.for_ObjectExtensions.when_comparing_expando_objects;

public class and_neither_operand_is_expando : Specification
{
    bool[] _results;

    void Because() => _results = [7.IsEqualTo(7L), 7.IsEqualTo("7"), "text".IsEqualTo("TEXT")];

    [Fact] void should_preserve_ordinary_scalar_equality() => _results.ShouldEachConformTo(result => !result);
}
