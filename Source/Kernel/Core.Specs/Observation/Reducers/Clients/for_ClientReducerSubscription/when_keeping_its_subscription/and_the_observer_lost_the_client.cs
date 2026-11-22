// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_keeping_its_subscription;

public class and_the_observer_lost_the_client : given.a_client_reducer_subscription_with_a_stored_definition
{
    Task Because() => _subscription.KeepSubscription(_connectionDefinition);

    [Fact] Task should_restore_the_subscription_with_the_definition_of_its_own_connection() => WasRestored(1);
}
