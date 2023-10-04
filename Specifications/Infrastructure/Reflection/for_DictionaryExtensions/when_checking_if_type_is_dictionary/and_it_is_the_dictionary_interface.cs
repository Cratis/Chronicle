// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reflection.for_DictionaryExtensions.when_checking_if_type_is_dictionary;

public class and_it_is_the_dictionary_interface : Specification
{
    bool result;

    void Because() => result = typeof(IDictionary<string, object>).IsDictionary();

    [Fact] void should_be_considered_a_dictionary() => result.ShouldBeTrue();
}
