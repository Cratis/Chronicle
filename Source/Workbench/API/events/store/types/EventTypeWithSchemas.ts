/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { EventTypeInformation } from './EventTypeInformation';

export class EventTypeWithSchemas {

    @field(EventTypeInformation)
    eventType!: EventTypeInformation;

    @field(Object, true)
    schemas!: any[];
}
