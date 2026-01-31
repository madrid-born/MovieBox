using System.ComponentModel.DataAnnotations;

namespace MovieBox.Models;

public class Movie
{
    [Key]
    public int Id { get; set; }
    public int ListId { get; set; }
    public string Title { get; set; }
    public int? Length { get; set; }
    public string? Language { get; set; }
    public int? Year {get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsSeen { get; set; }
    public string? Description { get; set; }
    public byte[]? Picture { get; set; }
    public virtual ICollection<CategorizedItems>? CategorizedItems { get; set; }
}