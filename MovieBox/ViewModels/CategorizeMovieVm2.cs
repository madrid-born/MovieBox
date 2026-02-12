using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.ViewModels;

public class CategorizeMovieVm2
{
    [Required]
    public int ListId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int? Length { get; set; }
    public string? Language { get; set; }
    public int? Year { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsSeen { get; set; }
    public string? Description { get; set; }

    // For categories (multi-select / checkbox list)
    public List<int> SelectedCategoryIds { get; set; } = new();

    // For dropdowns / checkbox rendering
    public List<SelectListItem> Lists { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();

    // For picture upload
    public IFormFile? PictureFile { get; set; }
}
