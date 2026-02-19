// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

(async () => {
    await import('reflect-metadata');
    const chai = await import('chai');
    global.expect = chai.expect;
    const should = chai.should();
    global.sinon = await import('sinon');
    const sinonChai = await import('sinon-chai');
    const chaiAsPromised = await import('chai-as-promised');
    chai.use(sinonChai.default);
     chai.use(chaiAsPromised.default);
})();
