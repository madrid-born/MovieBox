using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieBox.Models;
using MovieBox.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace MovieBox.Controllers;

public class MovieController(ApplicationDbContext db, IWebHostEnvironment env) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    #region Categorize

    [HttpGet]
    public async Task<IActionResult> Categorize(int? listId, int? movieId)
    {
        var vm = new CategorizeMovieVm();

        if (movieId.HasValue)
        {
            var movie = await db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == movieId.Value);
            if (movie == null) return NotFound();

            vm.Id = movie.Id;
            vm.ListId = movie.ListId;
            vm.Title = movie.Title;
            vm.Year = movie.Year;
            vm.Length = movie.Length;
            vm.Language = movie.Language;
            vm.Description = movie.Description;
            vm.IsAvailable = movie.IsAvailable ?? false;
            vm.IsSeen = movie.IsSeen ?? false;
            vm.LocalAddress = movie.LocalAddress;
            vm.ExistingPictureAddress = movie.PictureAddress;

            vm.SelectedCategoryIds = await db.CategorizedItems.Where(ci => ci.MovieId == movie.Id).Select(ci => ci.CategoryId).ToListAsync();
        }
        else
        {
            vm.ListId = listId;
        }

        await ReloadLookups(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Categorize(CategorizeMovieVm vm)
    {
        await ReloadLookups(vm);

        if (!ModelState.IsValid) return View(vm);
        if (!string.IsNullOrWhiteSpace(vm.NewLanguage))
        {
            vm.Language = vm.NewLanguage.Trim();
        }
        else
        {
            vm.Language = vm.Language?.Trim();
            if (vm.Language == "__other__") vm.Language = null;
        }

        if (string.IsNullOrWhiteSpace(vm.Language)) vm.Language = null;
        
        var listExists = await db.Lists.AnyAsync(l => l.Id == vm.ListId);
        if (!listExists)
        {
            ModelState.AddModelError(nameof(vm.ListId), "Selected list does not exist.");
            return View(vm);
        }

        if (vm.SelectedCategoryIds.Count > 0)
        {
            vm.SelectedCategoryIds = await db.Categories.Where(c => c.ListId == vm.ListId && vm.SelectedCategoryIds.Contains(c.Id))
                .Select(c => c.Id).ToListAsync();
        }

        Movie? movie;
        if (vm.Id.HasValue)
        {
            movie = await db.Movies.FirstOrDefaultAsync(m => m != null && m.Id == vm.Id.Value);
            if (movie == null) return NotFound();
            if (movie.ListId != vm.ListId!.Value)
            {
                ModelState.AddModelError(nameof(vm.ListId), "Movie does not belong to selected list.");
                return View(vm);
            }
        }
        else
        {
            movie = new Movie { IsDeleted = false };
            db.Movies.Add(movie);
        }

        if (vm.PictureFile is not null && vm.PictureFile.Length > 0)
        {
            movie.PictureAddress = await SaveResizedMovieImage(vm.PictureFile);
        }

        movie.ListId = vm.ListId!.Value;
        movie.Title = vm.Title.Trim();
        movie.Length = vm.Length;
        movie.Language = vm.Language;
        movie.Year = vm.Year;
        movie.IsAvailable = vm.IsAvailable;
        movie.IsSeen = vm.IsSeen;
        movie.Description = vm.Description?.Trim();
        movie.LocalAddress = vm.IsAvailable ? vm.LocalAddress?.Trim() : null;

        await db.SaveChangesAsync();

        var existing = await db.CategorizedItems.Where(ci => ci.MovieId == movie.Id).ToListAsync();
        if (existing.Count > 0) db.CategorizedItems.RemoveRange(existing);

        if (vm.SelectedCategoryIds.Count > 0)
        {
            var joinRows = vm.SelectedCategoryIds.Distinct()
                .Select(catId => new CategorizedItems { MovieId = movie.Id, CategoryId = catId });

            db.CategorizedItems.AddRange(joinRows);
        }

        await db.SaveChangesAsync();

        TempData["Success"] = vm.Id.HasValue ? "Movie updated!" : "Movie added and categorized!";
        return RedirectToAction(nameof(Categorize), new { listId = movie.ListId, movieId = movie.Id });
    }

    private async Task ReloadLookups(CategorizeMovieVm vm)
    {
        vm.Lists = await db.Lists.OrderBy(l => l.Name).Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync();

        if (vm.ListId is not null)
        {
            vm.Categories = await db.Categories.Where(c => c.ListId == vm.ListId).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
            
            var raw = await db.Movies.Where(m => m.ListId == vm.ListId && !string.IsNullOrWhiteSpace(m.Language))
                .Select(m => m.Language!.Trim()).ToListAsync();
            var distinct = raw.GroupBy(x => x.ToLower()).Select(g => g.First())
                .OrderBy(x => x).ToList();

            vm.Languages = distinct.Select(x => new SelectListItem { Value = x, Text = x }).ToList();
        }
        else
        {
            vm.Categories = [];
            vm.Languages = [];
        }
    }

        private async Task<string> SaveResizedMovieImage(IFormFile file)
        {
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid file type.");

            var uploadsRoot = Path.Combine(env.WebRootPath, "uploads", "movies");
            Directory.CreateDirectory(uploadsRoot);
            var fileName = $"{Guid.NewGuid():N}.jpg";
            var physicalPath = Path.Combine(uploadsRoot, fileName);
            await using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);
            
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(1000, 1500), Mode = ResizeMode.Crop }));
            await image.SaveAsJpegAsync(physicalPath);
            return $"/uploads/movies/{fileName}";
        }
    
    #endregion

    #region Filter

    [HttpGet]
    public async Task<IActionResult> Filter(
        int? listId,
        List<int>? selectedCategoryIds,
        int? minLength,
        int? maxLength,
        string? language,
        int? minYear,
        int? maxYear,
        bool? isAvailable,
        bool? isSeen,
        bool? isDeleted = false
    )
    {
        selectedCategoryIds ??= new List<int>();

        var vm = new FilterMoviesVm
        {
            ListId = listId,
            SelectedCategoryIds = selectedCategoryIds,
            IsDeleted = isDeleted,
            IsAvailable = isAvailable,
            IsSeen = isSeen,
            MinLength = minLength,
            MaxLength = maxLength,
            Language = language,
            MinYear = minYear,
            MaxYear = maxYear,
            Lists = await db.Lists.OrderBy(l => l.Name).Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync()
        };

        if (listId is null) return View(vm);

        vm.Categories = await db.Categories.Where(c => c.ListId == listId).OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

        var moviesQuery = db.Movies.Where(m => m.ListId == listId);

        if (selectedCategoryIds.Count > 0)
        {
            var validSelected = await db.Categories.Where(c => c.ListId == listId && selectedCategoryIds.Contains(c.Id))
                .Select(c => c.Id).ToListAsync();

            vm.SelectedCategoryIds = validSelected;
            var selectedCount = validSelected.Count;

            if (selectedCount == 0)
            {
                vm.Movies = [];
                return View(vm);
            }

            var movieIds = await db.CategorizedItems.Where(ci => validSelected.Contains(ci.CategoryId))
                .GroupBy(ci => ci.MovieId).Where(g => g.Select(x => x.CategoryId).Distinct().Count() == selectedCount)
                .Select(g => g.Key).ToListAsync();

            moviesQuery = moviesQuery.Where(m => movieIds.Contains(m.Id));
        }
        
        var languagesRaw = await db.Movies.Where(m => m.ListId == listId && !string.IsNullOrWhiteSpace(m.Language))
            .Select(m => m.Language!.Trim()).ToListAsync();

        var distinctLanguages = languagesRaw.GroupBy(l => l.ToLower())
            .Select(g => g.First()).OrderBy(l => l).ToList();

        vm.Languages = distinctLanguages.Select(l => new SelectListItem
            {
                Value = l,
                Text = l,
                Selected = vm.Language != null && l.Equals(vm.Language, StringComparison.OrdinalIgnoreCase)
            }).ToList();

        if (isDeleted.HasValue) moviesQuery = moviesQuery.Where(m => m.IsDeleted == isDeleted.Value);
        if (isAvailable.HasValue) moviesQuery = moviesQuery.Where(m => m.IsAvailable == isAvailable.Value);
        if (isSeen.HasValue) moviesQuery = moviesQuery.Where(m => m.IsSeen == isSeen.Value);
        if (minLength.HasValue) moviesQuery = moviesQuery.Where(m => m.Length.HasValue && m.Length.Value >= minLength.Value);
        if (maxLength.HasValue) moviesQuery = moviesQuery.Where(m => m.Length.HasValue && m.Length.Value <= maxLength.Value);
        if (!string.IsNullOrWhiteSpace(language))
        {
            var lang = language.Trim().ToLower();
            moviesQuery = moviesQuery.Where(m => m.Language != null && m.Language.Equals(lang, StringComparison.CurrentCultureIgnoreCase));
        }
        if (minYear.HasValue) moviesQuery = moviesQuery.Where(m => m.Year.HasValue && m.Year.Value >= minYear.Value);
        if (maxYear.HasValue) moviesQuery = moviesQuery.Where(m => m.Year.HasValue && m.Year.Value <= maxYear.Value);

        vm.Movies = await moviesQuery.OrderBy(m => m.Title).Select(m => new FilterMoviesVm.MovieRow
            {
                Id = m.Id,
                Title = m.Title,
                Year = m.Year,
                IsAvailable = m.IsAvailable,
                IsSeen = m.IsSeen,
                PictureAddress = m.PictureAddress
            }).ToListAsync();

        return View(vm);
    }

    #endregion

}
