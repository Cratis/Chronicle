// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModels.when_getting_paginated_instances.with_middle_page.context;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_getting_paginated_instances;

[Collection(ChronicleCollection.Name)]
public class with_middle_page(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : given.a_projection_with_many_instances(chronicleInProcessFixture)
    {
        public GetInstancesResponse Response;

        async Task Because()
        {
            await AppendAllEvents();

            var readModels = Services.GetRequiredService<IServices>().ReadModels;
            Response = await readModels.GetInstances(new GetInstancesRequest
            {
                EventStore = Constants.EventStore,
                Namespace = "Default",
                ReadModel = typeof(SomeReadModel).FullName,
                Page = 1,
                PageSize = 2
            });
        }
    }

    [Fact] void should_return_two_instances() => Context.Response.Instances.Count.ShouldEqual(2);
    [Fact] void should_return_total_count_of_five() => Context.Response.TotalCount.ShouldEqual(5L);
    [Fact] void should_echo_back_page_one() => Context.Response.Page.ShouldEqual(1);
    [Fact] void should_echo_back_page_size_two() => Context.Response.PageSize.ShouldEqual(2);
}
