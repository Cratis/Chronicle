/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { ParameterAttributes } from './ParameterAttributes';
import { CustomAttributeData } from './CustomAttributeData';
import { MemberInfo } from './MemberInfo';
import { Type } from './Type';

export class ParameterInfo {

    @field(Number)
    attributes!: ParameterAttributes;

    @field(CustomAttributeData, true)
    customAttributes!: CustomAttributeData[];

    @field(Object)
    defaultValue?: any;

    @field(Boolean)
    hasDefaultValue!: boolean;

    @field(Boolean)
    isIn!: boolean;

    @field(Boolean)
    isLcid!: boolean;

    @field(Boolean)
    isOptional!: boolean;

    @field(Boolean)
    isOut!: boolean;

    @field(Boolean)
    isRetval!: boolean;

    @field(MemberInfo)
    member!: MemberInfo;

    @field(Number)
    metadataToken!: number;

    @field(String)
    name?: string;

    @field(Type)
    parameterType!: Type;

    @field(Number)
    position!: number;

    @field(Object)
    rawDefaultValue?: any;
}
