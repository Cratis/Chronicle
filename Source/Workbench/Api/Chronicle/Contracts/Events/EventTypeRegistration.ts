/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventType } from './EventType';

export class EventTypeRegistration {

    @field(EventType)
    type!: EventType;

    @field(String)
    friendlyName!: string;

    @field(String)
    schema!: string;
}
