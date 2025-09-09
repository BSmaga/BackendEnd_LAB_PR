namespace Biblioteka.Domain;
public class Czytelnik
{
    public int Id { get; set; }
    public string Imie { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HaszHasla { get; set; } = string.Empty;
    public string Rola { get; set; } = "User";
}
