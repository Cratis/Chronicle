/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { MemberTypes } from './MemberTypes';
import { ParameterInfo } from './ParameterInfo';
import { Type } from './Type';
import { ICustomAttributeProvider } from './ICustomAttributeProvider';

export class MethodInfo {

    @field(Number)
    memberType!: MemberTypes;

    @field(ParameterInfo)
    returnParameter!: ParameterInfo;

    @field(Type)
    returnType!: Type;

    @field(Object, false, [
    ])
    returnTypeCustomAttributes!: ICustomAttributeProvider;
}
