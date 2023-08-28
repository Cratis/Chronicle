/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';

export class EventToAppend {

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;

    @field(Date)
    validFrom?: Date;
}
