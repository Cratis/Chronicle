/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventMetadata } from '../Events/EventMetadata';
import { EventContext } from '../Events/EventContext';

export class AppendedEventWithJsonAsContent {

    @field(EventMetadata)
    metadata!: EventMetadata;

    @field(EventContext)
    context!: EventContext;

    @field(Object)
    content!: any;
}
