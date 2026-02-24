using System.ComponentModel.DataAnnotations;
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
public class FilterMoviesVm
{
    public int? ListId { get; set; }
    public List<int> SelectedCategoryIds { get; set; } = new();
    public List<MovieRow> Movies { get; set; } = [];
    public List<SelectListItem> Lists { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();

    public bool? IsDeleted { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsSeen { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Language { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }

    public class MovieRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int? Year { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsSeen { get; set; }
        public string? PictureAddress { get; set; }
    }
}