/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { EventObservationState } from './EventObservationState';

export class EventContext {
    @field(String)
    eventSourceId!: string;
    @field(Number)
    sequenceNumber!: number;
    @field(Date)
    occurred!: Date;
    @field(Date)
    validFrom!: Date;
    @field(String)
    tenantId!: string;
    @field(String)
    correlationId!: string;
    @field(String)
    causationId!: string;
    @field(String)
    causedBy!: string;
    @field(EventObservationState)
    observationState!: EventObservationState;
}
