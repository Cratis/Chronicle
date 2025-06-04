/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { Guid } from '@cratis/fundamentals';
import { Causation } from '../Auditing/Causation';
import { Identity } from '../Identities/Identity';

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

    @field(Date)
    occurred!: Date;

    @field(Guid)
    correlationId!: Guid;

    @field(Causation, true)
    causation!: Causation[];

    @field(Identity)
    causedBy!: Identity;
}
