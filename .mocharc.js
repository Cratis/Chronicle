// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

require('reflect-metadata');

const chai = require('chai');
global.expect = chai.expect;
const should = chai.should();
global.sinon = require('sinon');
const sinonChai = require('sinon-chai');
const chaiAsPromised = require('chai-as-promised');
chai.use(sinonChai);
chai.use(chaiAsPromised);

module.exports = {
    diff: true,
    extension: ['ts'],
    spec: ['**/for_*/**/*.ts'],
    ignore: ['**/node_modules/**/*', '**/*.d.ts'],
    require: ['ts-node/register']
}
