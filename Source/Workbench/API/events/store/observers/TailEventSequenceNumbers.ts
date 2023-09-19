/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';

export class TailEventSequenceNumbers {

    @field(String)
    eventSequenceId!: string;

    @field(EventType, true)
    eventTypes!: EventType[];

    @field(Number)
    tail!: number;

    @field(Number)
    tailForEventTypes!: number;
}
