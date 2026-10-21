// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_Occurrences;

namespace Cratis.Chronicle.Kernel.Integration.Occurrences;

[Collection(SqlOccurrenceCollection.Name)]
public class when_starting_two_logical_instances(SqlOccurrenceFixture fixture)
{
    [Fact] public Task should_complete_one_startup_effect() => OccurrenceScenarios.LogicalStartup(fixture);
}
