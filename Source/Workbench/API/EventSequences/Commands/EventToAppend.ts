/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';


export class EventToAppend {

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;

    @field(Date)
    validFrom?: Date;
}
