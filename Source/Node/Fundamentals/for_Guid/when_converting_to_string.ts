// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../Guid';

describe('when converting to string', () => {
    const guid = Guid.create();
    const regex = new RegExp('^({{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}}{0,1})');

    const stringVersion = guid.toString();

    const match = stringVersion.match(regex);
    it('should be in the correct format', () => match?.should.not.be.null);
});
