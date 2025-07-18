// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Objects.for_ObjectExtensions;

public class when_ensuring_complex_path_with_multiple_arrays_and_nothing_defined : Specification
{
    TopLevel _initial;
    PropertyPath _propertyPath;
    ForthLevel _result;
    ArrayIndexer _firstArrayIndexer;
    ArrayIndexer _secondArrayIndexer;

    void Establish()
    {
        _initial = new(null);
        _propertyPath = new($"{nameof(TopLevel.FirstLevel)}.[{nameof(FirstLevel.SecondLevel)}].{nameof(SecondLevel.ThirdLevel)}.[{nameof(ThirdLevel.ForthLevel)}].{nameof(ForthLevel.SomeProperty)}");
        _firstArrayIndexer = new($"{nameof(TopLevel.FirstLevel)}.[{nameof(FirstLevel.SecondLevel)}]", "identifier", "first");
        _secondArrayIndexer = new($"{nameof(TopLevel.FirstLevel)}.[{nameof(FirstLevel.SecondLevel)}].{nameof(SecondLevel.ThirdLevel)}.[{nameof(ThirdLevel.ForthLevel)}]", "identifier", "second");
    }

    void Because() => _result = _initial.EnsurePath(_propertyPath, new ArrayIndexers([_firstArrayIndexer, _secondArrayIndexer])) as ForthLevel;

    [Fact] void should_return_a_new_object() => _result.ShouldNotBeNull();
    [Fact] void should_add_all_levels_and_include_returning_object() => _initial.FirstLevel.SecondLevel.First().ThirdLevel.ForthLevel.First().ShouldEqual(_result);
}
