using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QualityDoc.API.Models
{
    public class NewDocumentVersionViewModel
    {
        // El ID del Documento Maestro al que le vamos a colgar esta versión
        [Required]
        public int DocId { get; set; }

        // Para mostrarle al usuario qué documento está actualizando (Solo lectura)
        public string DocCode { get; set; } = string.Empty;
        public string DocName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes justificar el motivo de esta nueva versión para la auditoría.")]
        [Display(Name = "Motivo del Cambio")]
        [MaxLength(500)]
        public string ChangeDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Es obligatorio adjuntar el nuevo archivo.")]
        [Display(Name = "Nuevo Archivo (PDF/Word)")]
        public IFormFile NewFile { get; set; } = null!;
    }
}