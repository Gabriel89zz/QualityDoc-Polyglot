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
            // 🚀 CORRECCIÓN 1: Agregamos /admin para regresar al Login de C#
            return redirect(env('APP_URL') . '/admin/Auth/Login')->with('error', 'Acceso denegado. Se requiere iniciar sesión.');
        }

       
        $secretKey = env('JWT_SECRET');

        try {
            // 3. Validar y decodificar el token
            $decoded = JWT::decode($token, new Key($secretKey, 'HS256'));

            // 4. Si llegamos aquí, el token es 100% legítimo. 
            // Guardamos la identidad del operario en la sesión nativa de Laravel
            session([
                'is_authenticated' => true,
                'user_id' => $decoded->sub,
                'role' => $decoded->role,
                'name' => $decoded->name,
                'company_id' => $decoded->company_id ?? 0,
                'company_name' => $decoded->company_name ?? 'Empresa Desconocida',
                'dept_id' => $decoded->dept_id ?? 0
            ]);

            // 5. EL NUEVO CADENERO: Redirección inteligente basada en roles
            if (in_array($decoded->role, ['Administrador', 'Auditor'])) {
                return redirect()->route('dashboard'); 
            } else {
                return redirect()->route('dashboard'); 
            }

        // 🚀 PROTECCIÓN TOTAL: Cambiamos Exception por \Throwable para atrapar el error 500 de variables nulas
        } catch (\Throwable $e) {
            // 🚀 CORRECCIÓN 3: Agregamos /admin para expulsar a C#
            return redirect(env('APP_URL') . '/admin/Auth/Login?error=TokenInvalido');
        }
    }

    public function logout(Request $request)
    {
        // Limpiamos la sesión nativa de Laravel
        $request->session()->flush();
        
        // 🚀 CORRECCIÓN 4: Agregamos /admin para ir al LOGOUT del sistema central (C#)
       return redirect(env('APP_URL') . '/admin/Auth/Logout');
    }
}