/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { EventMetadata } from './EventMetadata';
import { EventContext } from './EventContext';
import { ExpandoObject } from './ExpandoObject';

export class AppendedEvent {

    @field(EventMetadata)
    metadata!: EventMetadata;

    @field(EventContext)
    context!: EventContext;

    @field(ExpandoObject, true)
    content!: ExpandoObject[];
}
