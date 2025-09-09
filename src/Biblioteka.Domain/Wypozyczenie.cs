namespace Biblioteka.Domain;
public class Wypozyczenie
{
    public int Id { get; set; }
    public int KsiazkaId { get; set; }
    public int CzytelnikId { get; set; }
    public DateTime DataWypozyczenia { get; set; } = DateTime.UtcNow;
    public DateTime? DataZwrotu { get; set; }
    public bool CzyZwrocona => DataZwrotu is not null;
}
