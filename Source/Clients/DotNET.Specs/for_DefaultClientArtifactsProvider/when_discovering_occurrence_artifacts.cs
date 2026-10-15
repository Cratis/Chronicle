// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.for_DefaultClientArtifactsProvider;

public class when_discovering_occurrence_artifacts : given.an_artifacts_provider
{
    void Establish() => _definedTypes = [typeof(TestTrigger).GetTypeInfo(), typeof(TestOccurrenceHandler).GetTypeInfo()];

    void Because() => _ = _provider.Triggers.ToArray();

    [Fact] void should_discover_typed_triggers() => _provider.Triggers.ShouldContainOnly(typeof(TestTrigger));
    [Fact] void should_discover_closed_handlers() => _provider.OccurrenceHandlers.ShouldContainOnly(typeof(TestOccurrenceHandler));
}
