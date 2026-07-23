// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.ReadModels.for_ReadModels.given;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing_instances_with_paging.given;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class instances_in_the_sink : all_dependencies
{
    protected const int TotalInstances = 15;
    protected List<string> _backingInstances = null!;
    protected Contracts.ReadModels.IMaterializedReadModels _materializedReadModelsService = null!;

    void Establish()
    {
        _projections.HasFor(typeof(PagedReadModel)).Returns(true);
        _reducers.HasFor(typeof(PagedReadModel)).Returns(false);

        _backingInstances = Enumerable.Range(0, TotalInstances)
            .Select(index => JsonSerializer.Serialize(new PagedReadModel { Name = $"Item{index}" }))
            .ToList();

        // Model the server contract: its effective offset is always Page * PageSize, and it returns
        // PageSize instances from that offset.
        _materializedReadModelsService = Substitute.For<Contracts.ReadModels.IMaterializedReadModels>();
        _materializedReadModelsService
            .ObserveInstances(Arg.Any<ObserveInstancesRequest>())
            .Returns(call =>
            {
                var request = call.Arg<ObserveInstancesRequest>();
                var offset = Math.Max(0, request.Page * request.PageSize);
                var page = _backingInstances.Skip(offset).Take(request.PageSize).ToList();
                var response = new ObserveInstancesResponse
                {
                    Instances = page,
                    TotalCount = _backingInstances.Count,
                    Page = request.Page,
                    PageSize = request.PageSize
                };
                return Observable.Return(response);
            });

        _services.MaterializedReadModels.Returns(_materializedReadModelsService);
    }

    public class PagedReadModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
