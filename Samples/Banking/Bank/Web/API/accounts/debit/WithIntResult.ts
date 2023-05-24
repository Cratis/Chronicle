/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/with-int-result');

export interface IWithIntResult {
}

export class WithIntResultValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
    };
}

export class WithIntResult extends Command<IWithIntResult, number> implements IWithIntResult {
    readonly route: string = '/api/accounts/debit/with-int-result';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new WithIntResultValidator();


    constructor() {
        super(Number, false);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
        ];
    }


    static use(initialValues?: IWithIntResult): [WithIntResult, SetCommandValues<IWithIntResult>, ClearCommandValues] {
        return useCommand<WithIntResult, IWithIntResult>(WithIntResult, initialValues);
    }
}
