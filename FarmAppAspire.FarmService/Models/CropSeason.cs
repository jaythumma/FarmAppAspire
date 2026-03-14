namespace FarmAppAspire.FarmService.Models;

public class CropSeason
{
    public Guid Id { get; set; }
    public Guid FieldId { get; set; }
    public string CropType { get; set; } = string.Empty;
    public string? Variety { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? ExpectedHarvestDate { get; set; }
    public CropSeasonStatus Status { get; set; } = CropSeasonStatus.Planned;

    public Field Field { get; set; } = null!;
}
