// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type StorageAuthorizationDiagnostic } from 'Features/Diagnostics';
import { describeStorageAuthorizationFailure, unauthorizedDatabases } from '../storageAuthorization';

const diagnostic = (database: string, collection: string, operation: string) => ({ database, collection, operation }) as StorageAuthorizationDiagnostic;
const template = '{operation} on {database}.{collection} was refused';

describe('when describing failures with no diagnostics', () => {
    let databases: string[];

    beforeEach(() => {
        databases = unauthorizedDatabases([]);
    });

    it('should report no unauthorized databases', () => {
        databases.should.be.empty;
    });
});

describe('when describing one denied collection', () => {
    let databases: string[];
    let description: string;

    beforeEach(() => {
        const failure = diagnostic('store-db', 'projection-x', 'createIndexes');
        databases = unauthorizedDatabases([failure]);
        description = describeStorageAuthorizationFailure(failure, template);
    });

    it('should list the database', () => {
        databases.should.deep.equal(['store-db']);
    });

    it('should name the operation database and collection', () => {
        description.should.equal('createIndexes on store-db.projection-x was refused');
    });
});

describe('when describing two collections in the same database', () => {
    let databases: string[];

    beforeEach(() => {
        databases = unauthorizedDatabases([
            diagnostic('store-db', 'one', 'listIndexes'),
            diagnostic('store-db', 'two', 'createIndexes')
        ]);
    });

    it('should list the database once', () => {
        databases.should.deep.equal(['store-db']);
    });
});
