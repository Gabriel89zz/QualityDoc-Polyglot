<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class AccessLog extends Model
{
    // 🚀 Le decimos qué campos se pueden llenar en masa
    protected $fillable = [
        'document_code',
        'document_title',
        'version_num',
        'user_id',
        'user_name',
        'user_role',
        'ip_address'
    ];
}