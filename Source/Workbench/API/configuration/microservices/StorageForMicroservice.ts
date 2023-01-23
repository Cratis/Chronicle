/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/cratis-fundamentals';

import { StorageTypes } from './StorageTypes';
import { StorageForTenants } from './StorageForTenants';

export class StorageForMicroservice {

    @field(StorageTypes, true)
    shared!: StorageTypes[];

    @field(StorageForTenants, true)
    tenants!: StorageForTenants[];
}
