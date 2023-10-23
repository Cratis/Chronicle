

import { PrimeReactProvider } from 'primereact/api';
import ReactDOM from 'react-dom/client';
import App from './App';
import React from 'react';
import './Styles/tailwind.css'
import './Styles/theme.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <PrimeReactProvider>
            <App />
        </PrimeReactProvider>
    </React.StrictMode>
);
