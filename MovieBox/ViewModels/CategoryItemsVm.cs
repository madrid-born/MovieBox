using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.ViewModels;

public class CategoryItemsVM
{
    public int? SelectedCategoryId { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();

    // results
    public List<MovieRow> Movies { get; set; } = new();

    public class MovieRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int? Year { get; set; }
        public int? Length { get; set; }
        public string? Language { get; set; }
        public string? ListName { get; set; }
        public bool? IsSeen { get; set; }
        public bool? IsAvailable { get; set; }
    }
}