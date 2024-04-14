/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventType } from './EventType';

export class EventTypeRegistration {

    @field(EventType)
    type!: EventType;

    @field(String)
    friendlyName!: string;

    @field(String)
    schema!: string;
}
