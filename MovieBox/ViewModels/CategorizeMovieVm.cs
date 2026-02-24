using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.ViewModels;


public class CategorizeMovieVm
{
    public int? Id { get; set; }
    public int? ListId { get; set; }
    public string Title { get; set; } = "";
    public int? Year { get; set; }
    public int? Length { get; set; }
    public string? Language { get; set; }
    public List<SelectListItem> Languages { get; set; } = new();
    public string? NewLanguage { get; set; }
    public string? Description { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsSeen { get; set; }
    public string? LocalAddress { get; set; }
    public List<int> SelectedCategoryIds { get; set; } = new();
    public IFormFile? PictureFile { get; set; }
    public string? ExistingPictureAddress { get; set; }
    public List<SelectListItem> Lists { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
}