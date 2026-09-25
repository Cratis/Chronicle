// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.for_TestingServices;

public class when_accessing_decision_read_service
{
    [Fact]
    public void should_not_expose_an_unreachable_decision_service_on_an_in_process_connection()
    {
        var eventStore = new EventStoreForTesting(null, Substitute.For<IClientArtifactsProvider>());
        try
        {
            var accessor = eventStore.Connection as IDecisionReadModelsServiceAccessor;
            accessor.ShouldNotBeNull();
            Assert.Throws<NotSupportedException>(() => accessor.DecisionReadModels);
        }
        finally
        {
            eventStore.Connection.Dispose();
        }
    }
}
