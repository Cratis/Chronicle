/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';

export class EventToApply {

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;
}
