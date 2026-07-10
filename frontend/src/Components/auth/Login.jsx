import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import authService from '../../Services/authService';
import logoAlbace from '../../assets/logo_albace_completo.png'; 
import './Login.css';

const Login = ({ onLoginSuccess }) => {
  const navigate = useNavigate();

  // 1. Estado único para las credenciales
  const [credentials, setCredentials] = useState({
    username: '',
    password: ''
  });

  // 2. Manejador de cambios en los inputs
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setCredentials({
      ...credentials,
      [name]: value
    });
  };

  // 3. Función de envío (Conexión real con tu .NET Core API)
  // const handleSubmit = async (e) => {
  //   e.preventDefault();
  //   console.log("Iniciando petición a .NET Core en puerto 5134...");

  //   try {
  //     // Usamos tu URL real
  //    //const url = 'http://192.168.100.6:5134/api/auth/login'; 
  //      const url = 'http://localhost:5134/api/auth/login'; 
      

  //      console.log("Enviando a:", url);



  //     const respuesta = await axios.post(url, {
  //       // Asegúrate que estos nombres coincidan con lo que espera tu DTO en C#
  //       Email: credentials.username,
  //       Password: credentials.password
  //     });

  //     // Si llegamos aquí, la API respondió 200 OK
  //     console.log("%c¡CONEXIÓN EXITOSA!", "color: green; font-size: 20px; font-weight: bold;");
  //     console.log("Token JWT:", respuesta.data.token);

  //     // Guardamos el token (paso opcional por ahora, pero útil)
  //     localStorage.setItem('token', respuesta.data.token);

  //     alert("¡Bienvenido a Metalúrgica Albace!");
      
  //     // Ejecutamos la función que viene de App.jsx para cambiar de pantalla
      
  //     onLoginSuccess();

  //   } catch (err) {
  //     console.error("Error en la conexión:", err.response?.data || err.message);
  //     alert("Error de autenticación: Verifica usuario/password o el CORS en tu API.");
  //   }
  // };
  
  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const resp = await authService.login(credentials.username, credentials.password);
      console.log('Login exitoso:', resp);
      if (resp?.token) localStorage.setItem('token', resp.token);
      onLoginSuccess();
      navigate('/');
    } catch (err) {
      console.error('Error en login:', err.response?.data || err.message || err);
      alert('Error de autenticación: Verifica usuario/password o que la API esté corriendo.');
    }
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <img src={logoAlbace} alt="Logo Albace" className="login-logo" />
          <h2>Gestión Administrativa Contable</h2>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Usuario</label>
            <input 
              type="text" 
              name="username" 
              value={credentials.username}
              onChange={handleInputChange}
              required 
            />
          </div>

          <div className="form-group">
            <label>Contraseña</label>
            <input 
              type="password" 
              name="password" 
              value={credentials.password}
              onChange={handleInputChange}
              required 
            />
          </div>

          <button type="submit" className="btn-primary">
            Ingresar al Sistema
          </button>
        </form>
      </div>
    </div>
  );
};

export default Login;