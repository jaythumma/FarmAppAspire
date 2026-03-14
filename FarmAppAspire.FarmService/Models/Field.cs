namespace FarmAppAspire.FarmService.Models;

public class Field
{
    public Guid Id { get; set; }
    public Guid FarmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal AreaHa { get; set; }
    public List<GpsPoint> GpsPolygon { get; set; } = [];
    public string? SoilType { get; set; }
    public FieldStatus Status { get; set; } = FieldStatus.Active;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public Farm Farm { get; set; } = null!;
    public ICollection<CropSeason> CropSeasons { get; set; } = [];
}
