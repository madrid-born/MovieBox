using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieBox.Models;
using MovieBox.ViewModels;

namespace MovieBox.Controllers;


public class MoviesController : Controller
{
    private readonly ApplicationDbContext _db;

    public MoviesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Movies/CategorizeMovie
    [HttpGet]
    public async Task<IActionResult> CategorizeMovie()
    {
        var vm = new CategorizeMovieVm
        {
            Lists = await _db.Lists
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync(),

            Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync()
        };

        return View(vm);
    }

    // POST: /Movies/CategorizeMovie
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategorizeMovie(CategorizeMovieVm vm)
    {
        // Re-load dropdown/checkbox sources if validation fails
        async Task ReloadLookups()
        {
            vm.Lists = await _db.Lists
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            vm.Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }

        if (!ModelState.IsValid)
        {
            await ReloadLookups();
            return View(vm);
        }

        // Optional: basic existence checks
        var listExists = await _db.Lists.AnyAsync(l => l.Id == vm.ListId);
        if (!listExists)
        {
            ModelState.AddModelError(nameof(vm.ListId), "Selected list does not exist.");
            await ReloadLookups();
            return View(vm);
        }

        if (vm.SelectedCategoryIds.Count > 0)
        {
            var validCategoryIds = await _db.Categories
                .Where(c => vm.SelectedCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            // Remove any ids that aren't real
            vm.SelectedCategoryIds = validCategoryIds;
        }

        byte[]? pictureBytes = null;
        if (vm.PictureFile is not null && vm.PictureFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await vm.PictureFile.CopyToAsync(ms);
            pictureBytes = ms.ToArray();
        }

        var movie = new Movie
        {
            ListId = vm.ListId,
            Title = vm.Title.Trim(),
            Length = vm.Length,
            Language = vm.Language?.Trim(),
            Year = vm.Year,
            IsAvailable = vm.IsAvailable,
            IsSeen = vm.IsSeen,
            Description = vm.Description?.Trim(),
            Picture = pictureBytes
        };

        _db.Movies.Add(movie);
        await _db.SaveChangesAsync(); // movie.Id is now generated

        if (vm.SelectedCategoryIds.Count > 0)
        {
            var joinRows = vm.SelectedCategoryIds
                .Distinct()
                .Select(catId => new CategorizedItems
                {
                    MovieId = movie.Id,
                    CategoryId = catId
                });

            _db.CategorizedItems.AddRange(joinRows);
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "Movie added and categorized!";
        return RedirectToAction(nameof(CategorizeMovie));
    }
}
