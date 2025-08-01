/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { EventContext } from './EventContext';

export class AppendedEvent {

    @field(EventContext)
    context!: EventContext;

    @field(String)
    content!: string;
}
