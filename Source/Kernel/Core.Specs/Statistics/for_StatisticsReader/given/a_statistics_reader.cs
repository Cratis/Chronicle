// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader.given;

public class a_statistics_reader : Specification
{
    protected IReadModels _readModels;
    protected INamespaces _namespaces;
    protected StatisticsReader _reader;

    void Establish()
    {
        _readModels = Substitute.For<IReadModels>();
        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns([]);
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<INamespaces>("store").Returns(_namespaces);
        _reader = new(_readModels, grainFactory);
    }
}
