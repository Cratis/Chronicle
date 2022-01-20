/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { AccountId } from './AccountId';
import { AccountName } from './AccountName';
import { PersonId } from './PersonId';

export type DebitAccount = {
    id: AccountId;
    name: AccountName;
    owner: PersonId;
    balance: number;
};
