/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { AppendedEventWithJsonAsContent } from './AppendedEventWithJsonAsContent';

export class PagedQueryResult {

    @field(AppendedEventWithJsonAsContent, true)
    items!: AppendedEventWithJsonAsContent[];

    @field(Number)
    totalCount!: number;
}
