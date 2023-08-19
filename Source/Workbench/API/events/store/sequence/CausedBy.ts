/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';


export class CausedBy {

    @field(String)
    subject!: string;

    @field(String)
    name!: string;

    @field(String)
    userName!: string;

    @field(CausedBy)
    onBehalfOf?: CausedBy;
}
