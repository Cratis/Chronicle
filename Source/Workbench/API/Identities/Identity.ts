/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from 'Infrastructure';

import { Identity } from './Identity';

export class Identity {

    @field(String)
    subject!: string;

    @field(String)
    name!: string;

    @field(String)
    userName!: string;

    @field(Identity)
    onBehalfOf!: Identity;
}
