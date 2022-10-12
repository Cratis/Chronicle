/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';
import { derivedType } from '@aksio/cratis-fundamentals';

import { AccountType } from './AccountType';

@derivedType('2c025801-2223-402c-a42a-893845bb1077')
export class DebitAccount {
    @field(String)
    id!: string;
    @field(String)
    name!: string;
    @field(Number)
    type!: AccountType;
}
