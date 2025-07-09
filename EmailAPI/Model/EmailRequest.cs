using System.ComponentModel.DataAnnotations;

namespace EmailAPI.Model
{
    public class EmailRequest
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O assunto é obrigatório.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "A mensagem é obrigatória.")]
        public string Message { get; set; } = string.Empty;
    }
}
