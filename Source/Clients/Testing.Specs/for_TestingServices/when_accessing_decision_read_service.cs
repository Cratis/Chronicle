// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_accessing_decision_read_service
{
    [Fact]
    public void should_expose_the_generated_service_on_an_in_process_connection()
    {
        var eventStore = new EventStoreForTesting(null, Substitute.For<IClientArtifactsProvider>());
        try
        {
            var accessor = eventStore.Connection as IDecisionReadModelsServiceAccessor;
            accessor.ShouldNotBeNull();
            accessor.DecisionReadModels.ShouldNotBeNull();
        }
        finally
        {
            eventStore.Connection.Dispose();
        }
    }
}
