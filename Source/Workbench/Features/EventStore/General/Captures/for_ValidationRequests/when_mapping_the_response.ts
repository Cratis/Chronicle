// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { validateSnapshot, type ValidationCommand } from '../validationRequests';
import { message } from './given/a_harness';

const commandReturning = (result: unknown) => {
    const command = { eventStore: '', declaration: '', execute: sinon.stub().resolves(result) };
    return command as typeof command & ValidationCommand;
};

describe('when mapping the validate response', () => {
    it('should set the snapshot on the command before executing', async () => {
        const command = commandReturning({ isSuccess: true, response: { messages: [] } });
        command.execute.callsFake(async () => {
            command.eventStore.should.equal('store');
            command.declaration.should.equal('capture A');
            return { isSuccess: true, response: { messages: [] } };
        });
        await validateSnapshot(command, { eventStore: 'store', declaration: 'capture A' });
        command.execute.should.be.calledOnce;
    });

    it('should return the messages of the response object', async () => {
        const command = commandReturning({ isSuccess: true, response: { messages: [message('x')] } });
        (await validateSnapshot(command, { eventStore: 's', declaration: 'd' }))!.should.deep.equal([message('x')]);
    });

    it('should return no messages when the response is empty', async () => {
        const command = commandReturning({ isSuccess: true, response: null });
        (await validateSnapshot(command, { eventStore: 's', declaration: 'd' }))!.should.deep.equal([]);
    });

    it('should return null when the request did not succeed', async () => {
        const command = commandReturning({ isSuccess: false, response: null });
        ((await validateSnapshot(command, { eventStore: 's', declaration: 'd' })) === null).should.be.true;
    });
});
