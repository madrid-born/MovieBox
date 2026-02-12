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
        var vm = new CategorizeMovieVm2
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
    public async Task<IActionResult> CategorizeMovie(CategorizeMovieVm2 vm2)
    {
        // Re-load dropdown/checkbox sources if validation fails
        async Task ReloadLookups()
        {
            vm2.Lists = await _db.Lists
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .ToListAsync();

            vm2.Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }

        if (!ModelState.IsValid)
        {
            await ReloadLookups();
            return View(vm2);
        }

        // Optional: basic existence checks
        var listExists = await _db.Lists.AnyAsync(l => l.Id == vm2.ListId);
        if (!listExists)
        {
            ModelState.AddModelError(nameof(vm2.ListId), "Selected list does not exist.");
            await ReloadLookups();
            return View(vm2);
        }

        if (vm2.SelectedCategoryIds.Count > 0)
        {
            var validCategoryIds = await _db.Categories
                .Where(c => vm2.SelectedCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            // Remove any ids that aren't real
            vm2.SelectedCategoryIds = validCategoryIds;
        }

        byte[]? pictureBytes = null;
        if (vm2.PictureFile is not null && vm2.PictureFile.Length > 0)
        {
            using var ms = new MemoryStream();
            await vm2.PictureFile.CopyToAsync(ms);
            pictureBytes = ms.ToArray();
        }

        var movie = new Movie
        {
            ListId = vm2.ListId,
            Title = vm2.Title.Trim(),
            Length = vm2.Length,
            Language = vm2.Language?.Trim(),
            Year = vm2.Year,
            IsAvailable = vm2.IsAvailable,
            IsSeen = vm2.IsSeen,
            Description = vm2.Description?.Trim(),
            // Picture = pictureBytes
        };

        _db.Movies.Add(movie);
        await _db.SaveChangesAsync(); // movie.Id is now generated

        if (vm2.SelectedCategoryIds.Count > 0)
        {
            var joinRows = vm2.SelectedCategoryIds
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
