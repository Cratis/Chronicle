// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

[Collection(OccurrenceCollection.Name)]
public class when_reconnecting_without_automatic_registration(OccurrenceFixture fixture)
{
    [Fact] public Task should_complete_an_occurrence_recorded_after_the_reconnect() => OccurrenceScenarios.ReconnectingWithoutAutomaticRegistration(fixture);
}
