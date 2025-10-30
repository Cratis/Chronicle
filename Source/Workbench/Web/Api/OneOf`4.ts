/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { field } from '@cratis/fundamentals';
import { BasicAuthorization } from './Observation/Webhooks/BasicAuthorization';
import { BearerTokenAuthorization } from './Observation/Webhooks/BearerTokenAuthorization';
import { None } from './None';
import { OAuthAuthorization } from './Observation/Webhooks/OAuthAuthorization';

export class OneOf`4 {

    @field(Object)
    value!: any;

    @field(Number)
    index!: number;

    @field(Boolean)
    isT0!: boolean;

    @field(Boolean)
    isT1!: boolean;

    @field(Boolean)
    isT2!: boolean;

    @field(Boolean)
    isT3!: boolean;

    @field(BasicAuthorization)
    asT0!: BasicAuthorization;

    @field(BearerTokenAuthorization)
    asT1!: BearerTokenAuthorization;

    @field(OAuthAuthorization)
    asT2!: OAuthAuthorization;

    @field(None)
    asT3!: None;
}
