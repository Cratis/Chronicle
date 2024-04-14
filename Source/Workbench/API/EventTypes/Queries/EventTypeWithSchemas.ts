/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { EventType } from '../EventType';

export class EventTypeWithSchemas {

    @field(EventType)
    type!: EventType;

    @field(Object, true)
    schemas!: any[];
}
