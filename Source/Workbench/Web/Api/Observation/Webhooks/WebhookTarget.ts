/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { BasicAuthorization } from './BasicAuthorization';
import { BearerTokenAuthorization } from './BearerTokenAuthorization';
import { OAuthAuthorization } from './OAuthAuthorization';

export class WebhookTarget {

    @field(String)
    url!: string;

    @field(Object)
    headers!: any;

    @field(BasicAuthorization)
    basicAuthorization!: BasicAuthorization;

    @field(BearerTokenAuthorization)
    bearerTokenAuthorization!: BearerTokenAuthorization;

    @field(OAuthAuthorization)
    OAuthAuthorization!: OAuthAuthorization;
}
