/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { Char } from './Char';

export class string {

    @field(Char)
    chars!: Char;

    @field(Number)
    length!: number;
}
