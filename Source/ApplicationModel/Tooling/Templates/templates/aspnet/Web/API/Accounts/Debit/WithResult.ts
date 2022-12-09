/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues, ClearCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import { UltimateResult } from './UltimateResult';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/with-result');

export interface IWithResult {
}

export class WithResultValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
    };
}

export class WithResult extends Command<IWithResult, UltimateResult> implements IWithResult {
    readonly route: string = '/api/accounts/debit/with-result';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new WithResultValidator();


    constructor() {
        super(UltimateResult, false);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
        ];
    }


    static use(initialValues?: IWithResult): [WithResult, SetCommandValues<IWithResult>, ClearCommandValues] {
        return useCommand<WithResult, IWithResult>(WithResult, initialValues);
    }
}
