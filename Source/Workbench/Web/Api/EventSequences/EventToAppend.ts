/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventType } from '../Contracts/Events/EventType';

export class EventToAppend {

    @field(EventType)
    eventType!: EventType;

    @field(Object)
    content!: any;
}
