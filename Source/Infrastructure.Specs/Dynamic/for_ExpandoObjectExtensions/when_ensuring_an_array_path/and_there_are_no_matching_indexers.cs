// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_there_are_no_matching_indexers : Specification
{
    ExpandoObject _state;
    Exception _exception;

    void Establish() => _state = new ExpandoObject();

    void Because() => _exception = Catch.Exception(() => _state.EnsurePath(new PropertyPath("[children].topic"), ArrayIndexers.NoIndexers));

    [Fact] void should_throw_missing_array_indexer() => _exception.ShouldBeOfExactType<MissingArrayIndexerForPropertyPath>();
}
