using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.ViewModels;

public class CategorizeMovieVm
{
    [Required]
    public int? ListId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    public int? Year { get; set; }
    public int? Length { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }

    public bool IsAvailable { get; set; }
    public bool IsSeen { get; set; }
    public string? LocalAddress { get; set; }
    public IFormFile? PictureFile { get; set; }

    public List<SelectListItem> Lists { get; set; } = [];
    public List<SelectListItem> Categories { get; set; } = [];
    public List<int> SelectedCategoryIds { get; set; } = [];
}