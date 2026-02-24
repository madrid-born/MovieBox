using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieBox.Models;
using MovieBox.ViewModels;

namespace MovieBox.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var lists = await db.Lists.Select(l => new { l.Id, l.Name }).OrderBy(l => l.Name).ToListAsync();

        var movieStats = await db.Movies.GroupBy(m => m.ListId).Select(g => new
        {
            ListId = g.Key,
            Total = g.Count(),
            Active = g.Count(m => !m.IsDeleted),
            Deleted = g.Count(m => m.IsDeleted),
            Available = g.Count(m => !m.IsDeleted && m.IsAvailable == true),
            Seen = g.Count(m => !m.IsDeleted && m.IsSeen == true),
        }).ToListAsync();

        var categoryStats = await db.Categories.GroupBy(c => c.ListId)
            .Select(g => new { ListId = g.Key, Categories = g.Count() }).ToListAsync();

        var languageStats = await db.Movies.Where(m => !string.IsNullOrWhiteSpace(m.Language))
            .GroupBy(m => m.ListId).Select(g => new
            {
                ListId = g.Key,
                Languages = g.Select(m => m.Language!.Trim().ToLower()).Distinct().Count()
            }).ToListAsync();

        var msDict = movieStats.ToDictionary(x => x.ListId);
        var csDict = categoryStats.ToDictionary(x => x.ListId, x => x.Categories);
        var lsDict = languageStats.ToDictionary(x => x.ListId, x => x.Languages);

        var vm = new HomeDashboardVm
        {
            Lists = lists.Select(l =>
            {
                msDict.TryGetValue(l.Id, out var ms);
                csDict.TryGetValue(l.Id, out var catCount);
                lsDict.TryGetValue(l.Id, out var langCount);

                return new HomeDashboardVm.ListStatsRow
                {
                    ListId = l.Id,
                    Name = l.Name,
                    TotalMovies = ms?.Total ?? 0,
                    ActiveMovies = ms?.Active ?? 0,
                    DeletedMovies = ms?.Deleted ?? 0,
                    AvailableMovies = ms?.Available ?? 0,
                    SeenMovies = ms?.Seen ?? 0,
                    CategoriesCount = catCount,
                    LanguagesCount = langCount
                };
            }).ToList()
        };

        return View(vm);
    }
}