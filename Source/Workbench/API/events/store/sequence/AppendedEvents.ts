/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';

export class AppendedEvents {

    @field(AppendedEventWithJsonAsContent, true)
    events!: AppendedEventWithJsonAsContent[];

    @field(Number)
    tailSequenceNumber!: number;
}
