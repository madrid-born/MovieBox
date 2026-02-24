using System.ComponentModel.DataAnnotations;

namespace MovieBox.ViewModels;

public class ListVm
{
    public List<ListRow> Lists { get; set; } = [];
    public LoadList Load {get; init; } = new();
    public int? EditId { get; init; }

    public class ListRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = "";
    }
}

public class LoadList
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";
}
