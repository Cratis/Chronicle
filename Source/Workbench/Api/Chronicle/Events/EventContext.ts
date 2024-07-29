/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Causation } from '../Auditing/Causation';
import { Identity } from '../Identities/Identity';
import { EventObservationState } from './EventObservationState';

export class EventContext {

    @field(String)
    eventSourceId!: string;

    @field(Number)
    sequenceNumber!: number;

    @field(Date)
    occurred!: Date;

    @field(String)
    eventStore!: string;

    @field(String)
    namespace!: string;

    @field(String)
    correlationId!: string;

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;

    @field(EventObservationState)
    observationState!: EventObservationState;
}
