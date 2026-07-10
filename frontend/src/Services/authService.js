import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ||
  (import.meta.env.VITE_API_URL ? import.meta.env.VITE_API_URL.replace(/\/api\/auth$/, '') : 'http://localhost:5000');

const API_URL = `${BASE_URL}/api/auth`;

const login = async (username, password) => {
  try {
    const response = await axios.post(`${API_URL}/login`, {
      username,
      password
    });
    
    console.log('Respuesta de la API:', response.data);
    
    if (response.data?.token) {
      localStorage.setItem('token', response.data.token);
    }

    return response.data;

  } catch (error) {
    console.error('Error durante el login:', error.response ? error.response.data : error.message);
    throw error;
  }
};

const authService = {
  login
};

export default authService;