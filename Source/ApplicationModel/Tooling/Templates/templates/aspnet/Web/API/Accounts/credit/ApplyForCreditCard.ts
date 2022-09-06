/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/apply');

export interface IApplyForCreditCard {
}

export class ApplyForCreditCardValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
    };
}

export class ApplyForCreditCard extends Command<IApplyForCreditCard> implements IApplyForCreditCard {
    readonly route: string = '/api/accounts/credit/apply';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new ApplyForCreditCardValidator();


    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
        ];
    }


    static use(initialValues?: IApplyForCreditCard): [ApplyForCreditCard, SetCommandValues<IApplyForCreditCard>] {
        return useCommand<ApplyForCreditCard, IApplyForCreditCard>(ApplyForCreditCard, initialValues);
    }
}
