/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { CustomAttributeData } from './CustomAttributeData';
import { Type } from './Type';
import { MemberTypes } from './MemberTypes';
import { Module } from './Module';

export class MemberInfo {

    @field(CustomAttributeData, true)
    customAttributes!: CustomAttributeData[];

    @field(Type)
    declaringType?: Type;

    @field(Boolean)
    isCollectible!: boolean;

    @field(Number)
    memberType!: MemberTypes;

    @field(Number)
    metadataToken!: number;

    @field(Module)
    module!: Module;

    @field(String)
    name!: string;

    @field(Type)
    reflectedType?: Type;
}
