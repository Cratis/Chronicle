/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { EventType } from './EventType';

export class EventMetadata {

    @field(Number)
    sequenceNumber!: number;

    @field(EventType)
    type!: EventType;
}
