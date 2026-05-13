<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Firebase\JWT\JWT;
use Firebase\JWT\Key;
use Exception;

class AuthController extends Controller
{
    public function verifyToken(Request $request)
    {
        // 1. Recibimos el token de la URL (?token=xyz...)
        $token = $request->query('token');

        if (!$token) {
            // Si alguien entra directo a la ruta sin token, lo pateamos de vuelta al Login de C#
            // Ajusta el puerto 5269 si tu C# corre en otro lado cuando pruebes
            return redirect('http://127.0.0.1:5269/Auth/Login')->with('error', 'Acceso denegado. Se requiere iniciar sesión.');
        }

        // 2. Traemos la llave secreta del .env
        $secretKey = env('JWT_SECRET');

        try {
            // 3. Validar y decodificar el token
            // Si el token fue modificado o ya pasaron sus 30 segundos de vida, esto lanzará un Exception
            $decoded = JWT::decode($token, new Key($secretKey, 'HS256'));

            // 4. Si llegamos aquí, el token es 100% legítimo. 
            // Guardamos la identidad del operario en la sesión nativa de Laravel
            session([
                'is_authenticated' => true,
                'user_id' => $decoded->sub,
                'role' => $decoded->role,
                'name' => $decoded->name,
                'company_id' => $decoded->company_id ?? 0
            ]);

            // 5. Redirigimos al panel principal limpiando la URL por seguridad
            return redirect()->route('dashboard');

        } catch (Exception $e) {
            // El token expiró o alguien intentó hackearlo. Lo regresamos al C#
            return redirect('http://127.0.0.1:5269/Auth/Login?error=TokenInvalido');
        }
    }

public function logout(Request $request)
{
    // Limpiamos la sesión nativa de Laravel
    $request->session()->flush();
    
    // 🚀 Redirigimos al LOGOUT del sistema central (C#) para destruir la cookie maestra
    return redirect('http://127.0.0.1:5269/Auth/Logout');
}
}