/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventType } from './EventType';

export class EventTypeRegistration {

    @field(EventType)
    type!: EventType;

    @field(String)
    friendlyName!: string;

    @field(Object)
    schema!: any;
}
