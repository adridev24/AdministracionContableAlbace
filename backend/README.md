# BudgetControl API

Backend API minimal para autenticación JWT.

1. Actualiza la cadena de conexión en `appsettings.json` (`ConnectionStrings:DefaultConnection`).
2. Actualiza la clave JWT en `appsettings.json` (`Jwt:Key`) con una clave fuerte de al menos 32 caracteres.
3. Ejecuta `dotnet restore` en la carpeta `backend`.
4. Crea la base de datos en SQL Server ejecutando `sql/create_users_roles.sql` (reemplaza el `PasswordHash` por uno generado con BCrypt).
5. Ejecuta la API: `dotnet run`.

Login: POST `/api/auth/login` con JSON `{ "username": "admin", "password": "YourPassword" }`.
