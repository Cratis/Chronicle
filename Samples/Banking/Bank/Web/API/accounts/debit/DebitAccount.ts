/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { Owner } from './Owner';

export class DebitAccount {

    @field(String)
    id!: string;

    @field(String)
    name!: string;

    @field(String)
    ownerId!: string;

    @field(Owner)
    owner!: Owner;

    @field(Number)
    balance?: number;

    @field(Boolean)
    hasCard!: boolean;

    @field(Date)
    lastUpdated!: Date;
}
