/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { ArrayIndexer } from './ArrayIndexer';

export class ArrayIndexers {

    @field(Number)
    count!: number;

    @field(Boolean)
    isEmpty!: boolean;

    @field(ArrayIndexer, true)
    all!: ArrayIndexer[];
}
