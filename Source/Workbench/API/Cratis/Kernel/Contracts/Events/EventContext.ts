/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';
import { Causation } from '../Auditing/Causation';
import { Identity } from '../Identities/Identity';
import { EventObservationState } from './EventObservationState';

export class EventContext {

    @field(String)
    eventSourceId!: string;

    @field(Number)
    sequenceNumber!: number;

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(SerializableDateTimeOffset)
    validFrom!: SerializableDateTimeOffset;

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
