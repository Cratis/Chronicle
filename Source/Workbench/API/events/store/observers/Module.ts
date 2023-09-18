/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { Assembly } from './Assembly';
import { CustomAttributeData } from './CustomAttributeData';
import { ModuleHandle } from './ModuleHandle';

export class Module {

    @field(Assembly)
    assembly!: Assembly;

    @field(CustomAttributeData, true)
    customAttributes!: CustomAttributeData[];

    @field(String)
    fullyQualifiedName!: string;

    @field(Number)
    mDStreamVersion!: number;

    @field(Number)
    metadataToken!: number;

    @field(ModuleHandle)
    moduleHandle!: ModuleHandle;

    @field(String)
    moduleVersionId!: string;

    @field(String)
    name!: string;

    @field(String)
    scopeName!: string;
}
