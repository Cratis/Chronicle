// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/arc.react/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { Guid } from '@cratis/fundamentals';

/**
 * Represents the command for requiring a user to change their password.
 */
export interface IRequirePasswordChange {
    
    /**
     * The user's unique identifier.
     */
    userId?: Guid;
}

export class RequirePasswordChangeValidator extends CommandValidator<IRequirePasswordChange> {
    constructor() {
        super();
    }
}

/**
 * Represents the command for requiring a user to change their password.
 */
export class RequirePasswordChange extends Command<IRequirePasswordChange> implements IRequirePasswordChange {
    readonly route: string = '/api/security/require-password-change';
    readonly validation: CommandValidator = new RequirePasswordChangeValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('userId', Guid, false),
    ];

    private _userId!: Guid;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    /**
     * The user's unique identifier.
     */
    get userId(): Guid {
        return this._userId;
    }

    set userId(value: Guid) {
        this._userId = value;
        this.propertyChanged('userId');
    }

    static use(initialValues?: IRequirePasswordChange): [RequirePasswordChange, SetCommandValues<IRequirePasswordChange>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<RequirePasswordChange, IRequirePasswordChange>(RequirePasswordChange, initialValues);
    }
}
