using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.ViewModels;

public class FilterMoviesVm
{
    public int? ListId { get; set; }
    public List<int> SelectedCategoryIds { get; set; } = new();
    public List<MovieRow> Movies { get; set; } = [];
    public List<SelectListItem> Lists { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Languages { get; set; } = new();
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