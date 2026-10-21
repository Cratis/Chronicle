// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[Collection(OccurrenceCollection.Name)]
public class when_starting_two_logical_instances(OccurrenceFixture fixture)
{
    [Fact] public Task should_complete_one_startup_effect() => OccurrenceScenarios.LogicalStartup(fixture);
}
