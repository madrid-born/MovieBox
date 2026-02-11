using Microsoft.AspNetCore.Mvc.Rendering;

public class CategoryIntersectionVM
{
    // Checkbox selection binds to this
    public List<int> SelectedCategoryIds { get; set; } = new();

    // Render checkboxes from this
    public List<SelectListItem> Categories { get; set; } = new();

    // Results
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