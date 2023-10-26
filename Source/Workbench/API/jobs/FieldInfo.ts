/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { FieldAttributes } from './FieldAttributes';
import { RuntimeFieldHandle } from './RuntimeFieldHandle';
import { Type } from './Type';
import { MemberTypes } from './MemberTypes';

export class FieldInfo {

    @field(Number)
    attributes!: FieldAttributes;

    @field(RuntimeFieldHandle)
    fieldHandle!: RuntimeFieldHandle;

    @field(Type)
    fieldType!: Type;

    @field(Boolean)
    isAssembly!: boolean;

    @field(Boolean)
    isFamily!: boolean;

    @field(Boolean)
    isFamilyAndAssembly!: boolean;

    @field(Boolean)
    isFamilyOrAssembly!: boolean;

    @field(Boolean)
    isInitOnly!: boolean;

    @field(Boolean)
    isLiteral!: boolean;

    @field(Boolean)
    isNotSerialized!: boolean;

    @field(Boolean)
    isPinvokeImpl!: boolean;

    @field(Boolean)
    isPrivate!: boolean;

    @field(Boolean)
    isPublic!: boolean;

    @field(Boolean)
    isSecurityCritical!: boolean;

    @field(Boolean)
    isSecuritySafeCritical!: boolean;

    @field(Boolean)
    isSecurityTransparent!: boolean;

    @field(Boolean)
    isSpecialName!: boolean;

    @field(Boolean)
    isStatic!: boolean;

    @field(Number)
    memberType!: MemberTypes;
}
