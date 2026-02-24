namespace MovieBox.ViewModels;

public class HomeDashboardVm
{
    public List<ListStatsRow> Lists { get; set; } = new();

    public class ListStatsRow
    {
        public int ListId { get; set; }
        public string Name { get; set; } = "";
        public int TotalMovies { get; set; }
        public int ActiveMovies { get; set; }
        public int DeletedMovies { get; set; }
        public int AvailableMovies { get; set; }
        public int SeenMovies { get; set; }
        public int CategoriesCount { get; set; }
        public int LanguagesCount { get; set; }
    }
}
