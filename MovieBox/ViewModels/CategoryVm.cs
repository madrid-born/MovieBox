using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.ViewModels;

public class CategoryVm
{
    public List<CategoryRow> Categories { get; set; } = [];
    public LoadCategory Load { get; init; } = new();
    public int? EditId { get; init; }
    public List<SelectListItem> Lists { get; set; } = [];

    public class CategoryRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = "";
        public int ListId { get; init; }
        public string ListTitle { get; init; } = "";
    }
}

public class LoadCategory
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";
    [Required]
    public int? ListId { get; init; }
}