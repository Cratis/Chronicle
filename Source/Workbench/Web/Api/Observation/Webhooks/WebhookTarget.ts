/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { OneOf`4 } from '../../OneOf`4';

export class WebhookTarget {

    @field(String)
    url!: string;

    @field(OneOf`4)
    authorization!: OneOf`4;

    @field(Object)
    headers!: any;
}
