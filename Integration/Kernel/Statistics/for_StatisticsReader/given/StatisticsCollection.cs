// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.Statistics.for_StatisticsReader.given;

/// <summary>
/// Serializes the statistics silo with collections sharing the process-wide client artifacts provider.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class StatisticsCollection : ICollectionFixture<StatisticsFixture>
{
    public const string Name = "Fresh store statistics";
}
