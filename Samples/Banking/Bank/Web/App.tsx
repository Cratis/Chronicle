// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { Navigation } from './Navigation';
import { Home } from './Home';
import { Ledger } from './Accounts/Debit/Ledger';
import { DebitAccounts } from './Accounts/Debit/DebitAccounts';
import { AccountHolders } from './AccountHolders/AccountHolders';
import { IdentityProvider } from '@aksio/applications/identity';

export const App = () => {
    return (
        <IdentityProvider>
            <Router>
                <div>
                    <div>
                        <Navigation />
                    </div>
                    <div style={{ width: '100%' }}>
                        <Routes>
                            <Route path="/" element={<Home />} />
                            <Route path="/accounts/debit" element={<DebitAccounts />} />
                            <Route path="/accounts/debit/:id/ledger" element={<Ledger />} />
                            <Route path="/integration" element={<AccountHolders />} />
                        </Routes>
                    </div>
                </div>
            </Router>
        </IdentityProvider>
    );
};
