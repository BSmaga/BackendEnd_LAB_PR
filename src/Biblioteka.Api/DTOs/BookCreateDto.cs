using System.ComponentModel.DataAnnotations;

namespace Biblioteka.Api.DTOs;

public class BookCreateDto
{
    [Required]
    public string Tytul { get; set; } = string.Empty;

    [Required]
    public string Autor { get; set; } = string.Empty;

    [Range(1000, 2100, ErrorMessage = "Rok musi być z przedziału 1000-2100")]
    public int Rok { get; set; }

    [Required]
    public string ISBN { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Liczba egzemplarzy nie może być ujemna")]
    public int LiczbaEgzemplarzy { get; set; }
}
