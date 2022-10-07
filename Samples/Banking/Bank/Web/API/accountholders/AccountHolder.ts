/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { Address } from './Address';

export class AccountHolder {
    @field(String)
    firstName!: string;
    @field(String)
    lastName!: string;
    @field(String)
    socialSecurityNumber!: string;
    @field(Address)
    address!: Address;
    @field(Date)
    lastUpdated!: Date;
}
