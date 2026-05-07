using System.ComponentModel.DataAnnotations;

namespace QualityDoc.API.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio para ingresar.")]
        [EmailAddress(ErrorMessage = "Debes ingresar un formato de correo válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }
}