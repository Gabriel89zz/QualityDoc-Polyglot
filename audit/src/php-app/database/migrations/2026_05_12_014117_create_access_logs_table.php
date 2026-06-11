<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up()
    {
        Schema::create('access_logs', function (Blueprint $table) {
            $table->id(); // Llave primaria autoincrementable
        
            // 🚀 Datos del documento consultado
            $table->string('document_code')->index(); // Ej. PR-001 (Le ponemos index para que las búsquedas sean veloces)
            $table->string('document_title');
            $table->string('version_num');

            // 🚀 Datos del usuario que lo consultó
            $table->integer('user_id'); 
            $table->string('user_name'); 
            $table->string('user_role');

            // 🚀 Datos de auditoría técnica
            $table->string('ip_address')->nullable();
        
            // Esto crea automáticamente las columnas 'created_at' (fecha de consulta) y 'updated_at'
            $table->timestamps(); 
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('access_logs');
    }
};
