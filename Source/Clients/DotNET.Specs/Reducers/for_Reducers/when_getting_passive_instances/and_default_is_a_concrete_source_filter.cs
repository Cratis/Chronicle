// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_default_is_a_concrete_source_filter : given.a_passive_reducer
{
    protected override Type ReducerType => typeof(DefaultSource);

    void Establish() => Seed(Event(1, 3, sourceType: "Default"), Event(2, 100, sourceType: "Orders"));

    Task Because() => ReadById();

    [Fact] void should_not_treat_default_as_unspecified() => _instance!.Amount.ShouldEqual(3);
}
