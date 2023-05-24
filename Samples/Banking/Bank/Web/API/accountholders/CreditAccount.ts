/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';
import { derivedType } from '@aksio/fundamentals';

import { AccountType } from './AccountType';

@derivedType('b67b4e5b-d192-404b-ba6f-9647202bd20e')
export class CreditAccount {

    @field(String)
    id!: string;

    @field(String)
    name!: string;

    @field(Number)
    type!: AccountType;
}
