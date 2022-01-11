// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BrowserRouter as Router, Route } from 'react-router-dom';
import { Navigation } from './Navigation';

import { default as styles } from './App.module.scss';
import { Microservices } from './microservices/Microservices';
import { People } from './GDPR/People';


export const App = () => {
    return (
        <Router>
            <div className={styles.appContainer}>
                <div className={styles.navigationBar}>
                    <Navigation />
                </div>
                <div style={{ width: '100%' }}>
                    <Route exact path="/">
                        Home
                    </Route>
                    <Route path="/microservices">
                        <Microservices/>
                    </Route>
                    <Route path="/gdpr/people">
                        <People/>
                    </Route>
                </div>
            </div>
        </Router>
    );
};

