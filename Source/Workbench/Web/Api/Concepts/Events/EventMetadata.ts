/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventType } from './EventType';

export class EventMetadata {

    @field(Number)
    sequenceNumber!: number;

    @field(EventType)
    type!: EventType;
}
