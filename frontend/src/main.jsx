import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App' // Aquí llama a App.jsx que acabamos de modificar
import './index.css'
import './shared/form.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)