/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
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
