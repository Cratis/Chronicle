// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Extensions.Dolittle.EventStore.for_MongoDBConventionFilter
{
    public class when_asking_to_include_for_random_convention_and_type_in_namespace_of_types_typically_included : Specification
    {
        readonly MongoDBConventionFilter filter = new();
        bool result;

        void Because() => result = filter.ShouldInclude("Random Convention Pack", null, typeof(Event));

        [Fact] void should_include_it() => result.ShouldBeTrue();
    }
}
