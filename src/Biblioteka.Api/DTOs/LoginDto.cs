using System.ComponentModel.DataAnnotations;

namespace Biblioteka.Api.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Haslo { get; set; } = string.Empty;
}
