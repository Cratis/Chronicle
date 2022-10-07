/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';


export class DebitAccount {
    @field(String)
    id!: string;
    @field(String)
    name!: string;
    @field(String)
    owner!: string;
    @field(Number)
    balance?: number;
    @field(Boolean)
    hasCard!: boolean;
    @field(Date)
    lastUpdated!: Date;
}
