/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventType } from './EventType';

export class EventTypeWithSchemas {

    @field(EventType)
    type!: EventType;

    @field(Object, true)
    schemas!: any[];
}
