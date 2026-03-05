// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventsSeeding as EventsSeedingComponent } from '../../EventsSeeding';
import { useParams } from 'react-router-dom';

export const EventsSeeding = () => {
    const { eventStore, namespace } = useParams();

    return <EventsSeedingComponent eventStore={eventStore!} namespace={namespace} />;
};
