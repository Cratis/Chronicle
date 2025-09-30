/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { AuthenticationType } from './AuthenticationType';

export class WebhookTarget {

    @field(String)
    url!: string;

    @field(Number)
    authentication!: AuthenticationType;

    @field(String)
    username!: string;

    @field(String)
    password!: string;

    @field(String)
    bearerToken!: string;

    @field(Object)
    headers!: any;
}
