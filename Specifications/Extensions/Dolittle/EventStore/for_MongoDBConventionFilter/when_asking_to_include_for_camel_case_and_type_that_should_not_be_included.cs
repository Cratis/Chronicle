// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.MongoDB;

namespace Cratis.Extensions.Dolittle.EventStore.for_MongoDBConventionFilter
{
    public class when_asking_to_include_for_camel_case_and_type_that_should_not_be_included : Specification
    {
        readonly MongoDBConventionFilter filter = new();
        bool result;

        void Because() => result = filter.ShouldInclude(ConventionPacks.CamelCase, null, typeof(Event));

        [Fact] void should_not_include_it() => result.ShouldBeFalse();
    }
}
