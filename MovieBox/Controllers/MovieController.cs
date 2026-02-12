using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieBox.Models;
using MovieBox.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace MovieBox.Controllers;

public class MovieController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MovieController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Categorize(int? listId)
    {
        var vm = new CategorizeMovieVm
        {
            ListId = listId
        };

        await ReloadLookups(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Categorize(CategorizeMovieVm vm)
    {
        await ReloadLookups(vm);

        if (!ModelState.IsValid) return View(vm);

        var listExists = await _db.Lists.AnyAsync(l => l.Id == vm.ListId);
        if (!listExists)
        {
            ModelState.AddModelError(nameof(vm.ListId), "Selected list does not exist.");
            return View(vm);
        }

        if (vm.SelectedCategoryIds.Count > 0)
        {
            vm.SelectedCategoryIds = await _db.Categories
                .Where(c => c.ListId == vm.ListId && vm.SelectedCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
        }

        // Save image -> resize to 400x600 -> store path only
        string? pictureAddress = null;
        if (vm.PictureFile is not null && vm.PictureFile.Length > 0)
        {
            pictureAddress = await SaveResizedMovieImage(vm.PictureFile);
        }

        var movie = new Movie
        {
            IsDeleted = false,
            ListId = vm.ListId!.Value,
            Title = vm.Title.Trim(),
            Length = vm.Length,
            Language = vm.Language?.Trim(),
            Year = vm.Year,
            IsAvailable = vm.IsAvailable, // model is bool? -> ok
            IsSeen = vm.IsSeen,
            Description = vm.Description?.Trim(),
            PictureAddress = pictureAddress,
            LocalAddress = vm.IsAvailable ? vm.LocalAddress?.Trim() : null
        };

        _db.Movies.Add(movie);
        await _db.SaveChangesAsync();

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
        return RedirectToAction(nameof(Categorize), new { listId = vm.ListId });
    }

    private async Task ReloadLookups(CategorizeMovieVm vm)
    {
        vm.Lists = await _db.Lists
            .OrderBy(l => l.Name)
            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
            .ToListAsync();

        if (vm.ListId is not null)
        {
            vm.Categories = await _db.Categories
                .Where(c => c.ListId == vm.ListId)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }
        else
        {
            vm.Categories = [];
        }
    }

    private async Task<string> SaveResizedMovieImage(IFormFile file)
    {
        // simple validation
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file type.");

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "movies");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}.jpg";
        var physicalPath = Path.Combine(uploadsRoot, fileName);

        using var stream = file.OpenReadStream();
        using var image = await Image.LoadAsync(stream);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(1000, 1500),
            Mode = ResizeMode.Crop
        }));

        await image.SaveAsJpegAsync(physicalPath);

        // this is what goes into DB
        return $"/uploads/movies/{fileName}";
    }
}
