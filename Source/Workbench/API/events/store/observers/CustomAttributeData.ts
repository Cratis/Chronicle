/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { Type } from './Type';
import { ConstructorInfo } from './ConstructorInfo';
import { CustomAttributeTypedArgument } from './CustomAttributeTypedArgument';
import { CustomAttributeNamedArgument } from './CustomAttributeNamedArgument';

export class CustomAttributeData {

    @field(Type)
    attributeType!: Type;

    @field(ConstructorInfo)
    constructor!: ConstructorInfo;

    @field(CustomAttributeTypedArgument, true)
    constructorArguments!: CustomAttributeTypedArgument[];

    @field(CustomAttributeNamedArgument, true)
    namedArguments!: CustomAttributeNamedArgument[];
}
