// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ClientObserversState.given;

public class client_observers_state : Specification
{
    protected const string connection_id = "1b7d147e-edca-4b1b-8ea2-a95f7905a756";
    protected const string observer_id = "17b543cc-7f3b-4c83-b2bf-6f008ea7f476";
    protected const string microservice_id = "98449e14-49cd-4ce5-9d9f-a95cb541c6bd";
    protected const string tenant_id = "b62fc376-ea79-48db-8cb4-483ad2a0ad24";
    protected const string event_sequence_id = "df492dc6-6ba9-4cd0-87dc-d2f884af6878";
    protected ClientObserversState state;

    void Establish() => state = new();
}
