namespace Biblioteka.Api.DTOs;

public class CzytelnikDto
{
    public int Id { get; set; }
    public string Imie { get; set; } = "";
    public string Email { get; set; } = "";
    public string Rola { get; set; } = "User";
}

public class CzytelnikCreateDto
{
    public string Imie { get; set; } = "";
    public string Email { get; set; } = "";
    public string Haslo { get; set; } = "";
    public string Rola { get; set; } = "User";
}
