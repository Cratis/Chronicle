// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Integration.Specifications.for_ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_paginated_instances.with_page_beyond_total.context;
using given = Cratis.Chronicle.Integration.Specifications.for_ReadModels.given;

namespace Cratis.Chronicle.InProcess.Integration.for_ReadModels.when_getting_paginated_instances;

[Collection(ChronicleCollection.Name)]
public class with_page_beyond_total(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_with_many_instances(chronicleInProcessFixture)
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
                Page = 10,
                PageSize = 2
            });
        }
    }

    [Fact] void should_return_no_instances() => Context.Response.Instances.ShouldBeEmpty();
    [Fact] void should_return_total_count_of_five() => Context.Response.TotalCount.ShouldEqual(5L);
    [Fact] void should_echo_back_page_ten() => Context.Response.Page.ShouldEqual(10);
    [Fact] void should_echo_back_page_size_two() => Context.Response.PageSize.ShouldEqual(2);
}
