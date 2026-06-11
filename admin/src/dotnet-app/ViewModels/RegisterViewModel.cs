using System.ComponentModel.DataAnnotations;

namespace QualityDoc.API.ViewModels
{
    public class RegisterViewModel
    {
        // Datos de la Empresa
        [Required(ErrorMessage = "La razón social es obligatoria")]
        [Display(Name = "Nombre de la Empresa")]
        public string LegalName { get; set; }

        [Required(ErrorMessage = "El RFC/Tax ID es obligatorio")]
        public string TaxId { get; set; }

        // Datos del Administrador
        [Required(ErrorMessage = "Tu nombre es obligatorio")]
        public string AdminFullName { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}