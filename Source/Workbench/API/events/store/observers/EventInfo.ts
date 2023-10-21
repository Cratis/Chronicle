/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { field } from '@aksio/fundamentals';

import { MethodInfo } from './MethodInfo';
import { EventAttributes } from './EventAttributes';
import { Type } from './Type';
import { MemberTypes } from './MemberTypes';

export class EventInfo {

    @field(MethodInfo)
    addMethod?: MethodInfo;

    @field(Number)
    attributes!: EventAttributes;

    @field(Type)
    eventHandlerType?: Type;

    @field(Boolean)
    isMulticast!: boolean;

    @field(Boolean)
    isSpecialName!: boolean;

    @field(Number)
    memberType!: MemberTypes;

    @field(MethodInfo)
    raiseMethod?: MethodInfo;

    @field(MethodInfo)
    removeMethod?: MethodInfo;
}
