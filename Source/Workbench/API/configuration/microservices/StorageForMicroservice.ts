/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { StorageType } from './StorageType';
import { StorageTypes } from './StorageTypes';

export class StorageForMicroservice {

    @field(Object)
    shared!: [key: string, value: StorageType];

    @field(Object)
    tenants!: [key: string, value: StorageTypes];
}
