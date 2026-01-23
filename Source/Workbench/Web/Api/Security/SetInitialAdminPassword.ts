// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator } from '@cratis/arc/commands';
import { useCommand, SetCommandValues, ClearCommandValues } from '@cratis/arc.react/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { Guid } from '@cratis/fundamentals';

/**
 * Represents the command for setting the initial admin password.
 */
export interface ISetInitialAdminPassword {
    
    /**
     * The confirmed new password.
     */
    confirmedPassword?: string;
    
    /**
     * The new password.
     */
    password?: string;
    
    /**
     * The user's unique identifier.
     */
    userId?: Guid;
}

export class SetInitialAdminPasswordValidator extends CommandValidator<ISetInitialAdminPassword> {
    constructor() {
        super();
    }
}

/**
 * Represents the command for setting the initial admin password.
 */
export class SetInitialAdminPassword extends Command<ISetInitialAdminPassword> implements ISetInitialAdminPassword {
    readonly route: string = '/api/security/set-initial-admin-password';
    readonly validation: CommandValidator = new SetInitialAdminPasswordValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('confirmedPassword', String, false),
        new PropertyDescriptor('password', String, false),
        new PropertyDescriptor('userId', Guid, false),
    ];

    private _confirmedPassword!: string;
    private _password!: string;
    private _userId!: Guid;

    constructor() {
        super(Object, false);
    }

    get requestParameters(): string[] {
        return [];
    }

    /**
     * The confirmed new password.
     */
    get confirmedPassword(): string {
        return this._confirmedPassword;
    }

    set confirmedPassword(value: string) {
        this._confirmedPassword = value;
        this.propertyChanged('confirmedPassword');
    }

    /**
     * The new password.
     */
    get password(): string {
        return this._password;
    }

    set password(value: string) {
        this._password = value;
        this.propertyChanged('password');
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

    static use(initialValues?: ISetInitialAdminPassword): [SetInitialAdminPassword, SetCommandValues<ISetInitialAdminPassword>, ClearCommandValues] {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        return useCommand<SetInitialAdminPassword, ISetInitialAdminPassword>(SetInitialAdminPassword, initialValues);
    }
}
