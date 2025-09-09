namespace Biblioteka.Api.DTOs;

public class BookDto
{
    public int Id { get; set; }
    public string Tytul { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public int Rok { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public int LiczbaEgzemplarzy { get; set; }
}
