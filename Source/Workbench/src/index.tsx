

import { PrimeReactProvider } from 'primereact/api';
import ReactDOM from 'react-dom/client';
import App from './App';
import React from 'react';
import './index.css'
import './Styles/theme.css'
import 'primeicons/primeicons.css';



ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider>
            <App />
        </PrimeReactProvider>
    </React.StrictMode>
);
