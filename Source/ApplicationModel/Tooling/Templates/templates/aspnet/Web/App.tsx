
import { BrowserRouter as Router, Route } from 'react-router-dom';
import { default as styles } from './App.module.scss';
import { Navigation } from './Navigation';
import { Home } from './Home';
import { DebitAccounts } from './Accounts/Debit/DebitAccounts';

export const App = () => {
    return (
        <Router>
            <div className={styles.appContainer}>
                <div className={styles.navigationBar}>
                    <Navigation />
                </div>
                <div style={{ width: '100%' }}>
                    <Route exact path="/">
                        <Home/>
                    </Route>
                    <Route path="/accounts/debit">
                        <DebitAccounts/>
                    </Route>
                </div>
            </div>
        </Router>
    );
};
