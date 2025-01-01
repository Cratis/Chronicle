/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { Causation } from '../Auditing/Causation';
import { EventObservationState } from './EventObservationState';
import { Identity } from '../Identities/Identity';
import { SerializableDateTimeOffset } from '../Primitives/SerializableDateTimeOffset';

export class EventContext {

    @field(String)
    eventSourceType!: string;

    @field(String)
    eventSourceId!: string;

    @field(Number)
    sequenceNumber!: number;

    @field(String)
    eventStreamType!: string;

    @field(String)
    eventStreamId!: string;

    @field(SerializableDateTimeOffset)
    occurred!: SerializableDateTimeOffset;

    @field(String)
    eventStore!: string;

    @field(String)
    namespace!: string;

    @field(Guid)
    correlationId!: Guid;

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;

    @field(Number)
    observationState!: EventObservationState;
}
