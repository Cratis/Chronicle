/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { EventMetadata } from './EventMetadata';
import { EventContext } from './EventContext';
import { JsonObject } from './JsonObject';

export class AppendedEvent {
    @field(EventMetadata)
    metadata!: EventMetadata;
    @field(EventContext)
    context!: EventContext;
    @field(JsonObject, true)
    content!: JsonObject[];
}
