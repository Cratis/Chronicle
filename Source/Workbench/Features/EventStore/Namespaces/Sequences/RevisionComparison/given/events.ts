// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent, EventContext, EventRevision, EventType } from 'Features/Sequences';
import { ComparisonScope } from '../ComparisonScope';

export const scope: ComparisonScope = { eventStore: 'store', namespace: 'default', eventSequenceId: 'event-log' };

export const revision = (content: string, generation: number, minute: number, correlationId = `corr-${minute}`): EventRevision =>
    Object.assign(new EventRevision(), {
        generation,
        correlationId,
        occurred: new Date(Date.UTC(2024, 0, 15, 10, minute)),
        content
    });

export const eventWith = (revisions: EventRevision[], eventTypeId = 'order-placed', id = '42'): AppendedEvent =>
    Object.assign(new AppendedEvent(), {
        id,
        context: Object.assign(new EventContext(), {
            eventType: Object.assign(new EventType(), { id: eventTypeId, generation: 1 }),
            sequenceNumber: Number(id),
            occurred: new Date(Date.UTC(2024, 0, 15, 10, 0)),
            correlationId: 'corr-0'
        }),
        content: '{"quantity":3}',
        originalContent: '{"quantity":1,"status":"new"}',
        revisions
    });
