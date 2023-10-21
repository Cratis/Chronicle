/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { PropertyAttributes } from './PropertyAttributes';
import { MethodInfo } from './MethodInfo';
import { MemberTypes } from './MemberTypes';
import { Type } from './Type';

export class PropertyInfo {

    @field(Number)
    attributes!: PropertyAttributes;

    @field(Boolean)
    canRead!: boolean;

    @field(Boolean)
    canWrite!: boolean;

    @field(MethodInfo)
    getMethod?: MethodInfo;

    @field(Boolean)
    isSpecialName!: boolean;

    @field(Number)
    memberType!: MemberTypes;

    @field(Type)
    propertyType!: Type;

    @field(MethodInfo)
    setMethod?: MethodInfo;
}
