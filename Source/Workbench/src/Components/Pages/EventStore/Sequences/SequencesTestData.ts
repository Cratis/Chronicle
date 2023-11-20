// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEventWithJsonAsContent } from 'API/events/store/sequence/AppendedEventWithJsonAsContent';

export class SequencesTestData {

    static GetEvents(count: number): AppendedEventWithJsonAsContent[] {
        const events: AppendedEventWithJsonAsContent[] = [];
        for (let index = 0; index < count; index++) {
            events.push({
                metadata: {
                    sequenceNumber: index,
                    type: {
                        id: 'something',
                        generation: 0,
                        isPublic: false
                    }
                },
                context: {
                    eventSourceId: '',
                    sequenceNumber: 0,
                    occurred: new Date(),
                    validFrom: new Date(),
                    tenantId: '',
                    correlationId: '',
                    causation: [],
                    causedBy: {
                        subject: '',
                        name: '',
                        userName: ''
                    },
                    observationState: 0
                },
                content: {

                }
            });
        }
        return events;
    }
}
