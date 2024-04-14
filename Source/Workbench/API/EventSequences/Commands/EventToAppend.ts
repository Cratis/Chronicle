/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventType } from '../../EventTypes/EventType';

export class EventToAppend {

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;

    @field(Date)
    validFrom?: Date;
}
