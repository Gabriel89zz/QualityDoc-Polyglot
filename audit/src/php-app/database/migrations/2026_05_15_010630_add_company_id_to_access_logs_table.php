<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
       Schema::table('access_logs', function (Blueprint $table) {
        // Agregamos la columna entera, con valor por defecto 0 para que no rompa los logs viejos
        $table->integer('company_id')->default(0)->after('ip_address');
    });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
       Schema::table('access_logs', function (Blueprint $table) {
        $table->dropColumn('company_id');
    });
    }
};
