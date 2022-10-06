/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { EventObservationState } from './EventObservationState';

export type EventContext = {
    eventSourceId: string;
    sequenceNumber: number;
    occurred: Date;
    validFrom: Date;
    tenantId: string;
    correlationId: string;
    causationId: string;
    causedBy: string;
    observationState: EventObservationState;
};
