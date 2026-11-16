// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { buildViews } from '../graphViews';
import { organization } from './given/organization';

describe('when building views for a label the graph does not have', () => {
    it('should build no views', () => (buildViews(organization, 'Nobody', 1) === undefined).should.be.true);
});
