// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { generateCode, generationFailedCode, type GenerationCommand } from '../codeGenerationRequests';
import { context } from './given/a_harness';

const commandReturning = (response: { code?: string } | null) => {
    const command = { eventStore: '', namespace: '', declaration: '', language: '', draftReadModel: undefined, execute: sinon.stub().resolves({ response }) };
    return command as typeof command & GenerationCommand;
};

describe('when generating both outputs', () => {
    it('should set the context on each command', async () => {
        const declarative = commandReturning({ code: 'x' });
        const modelBound = commandReturning({ code: 'y' });
        await generateCode(declarative, modelBound, context('decl', 'Kotlin'));
        [declarative, modelBound].every(c => c.declaration === 'decl' && c.language === 'Kotlin' && c.eventStore === 'store' && c.namespace === 'ns').should.be.true;
    });

    it('should return both codes as a pair', async () => {
        const pair = await generateCode(commandReturning({ code: 'x' }), commandReturning({ code: 'y' }), context('decl'));
        pair.should.deep.equal({ declarative: 'x', modelBound: 'y' });
    });

    it('should use the failure text for an output without code', async () => {
        const pair = await generateCode(commandReturning(null), commandReturning({ code: 'y' }), context('decl'));
        pair.should.deep.equal({ declarative: generationFailedCode, modelBound: 'y' });
    });

    it('should clear a draft from an earlier request', async () => {
        const declarative = commandReturning({ code: 'x' });
        declarative.draftReadModel = {} as never;
        await generateCode(declarative, commandReturning({ code: 'y' }), context('decl'));
        (declarative.draftReadModel === undefined).should.be.true;
    });
});
