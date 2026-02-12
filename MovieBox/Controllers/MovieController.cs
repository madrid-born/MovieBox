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
        public async Task<IActionResult> Categorize(int? listId)
        {
            var vm = new CategorizeMovieVm { ListId = listId };

            await ReloadLookups(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Categorize(CategorizeMovieVm vm)
        {
            await ReloadLookups(vm);

            if (!ModelState.IsValid) return View(vm);

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

            string? pictureAddress = null;
            if (vm.PictureFile is not null && vm.PictureFile.Length > 0) pictureAddress = await SaveResizedMovieImage(vm.PictureFile);

            var movie = new Movie
            {
                IsDeleted = false,
                ListId = vm.ListId!.Value,
                Title = vm.Title.Trim(),
                Length = vm.Length,
                Language = vm.Language?.Trim(),
                Year = vm.Year,
                IsAvailable = vm.IsAvailable,
                IsSeen = vm.IsSeen,
                Description = vm.Description?.Trim(),
                PictureAddress = pictureAddress,
                LocalAddress = vm.IsAvailable ? vm.LocalAddress?.Trim() : null
            };

            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            if (vm.SelectedCategoryIds.Count > 0)
            {
                var joinRows = vm.SelectedCategoryIds.Distinct()
                    .Select(catId => new CategorizedItems { MovieId = movie.Id, CategoryId = catId });

                db.CategorizedItems.AddRange(joinRows);
                await db.SaveChangesAsync();
            }

            TempData["Success"] = "Movie added and categorized!";
            return RedirectToAction(nameof(Categorize), new { listId = vm.ListId });
        }

        private async Task ReloadLookups(CategorizeMovieVm vm)
        {
            vm.Lists = await db.Lists.OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync();

            if (vm.ListId is not null)
            {
                vm.Categories = await db.Categories.Where(c => c.ListId == vm.ListId).OrderBy(c => c.Name)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();
            }
            else
            {
                vm.Categories = [];
            }
        }

        private async Task<string> SaveResizedMovieImage(IFormFile file)
        {
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid file type.");

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
        public async Task<IActionResult> Filter(int? listId, List<int>? selectedCategoryIds)
        {
            selectedCategoryIds ??= [];

            var vm = new FilterMoviesVm
            {
                ListId = listId,
                SelectedCategoryIds = selectedCategoryIds,
                Lists = await db.Lists.OrderBy(l => l.Name)
                    .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync()
            };

            if (listId is null) return View(vm);

            vm.Categories = await db.Categories.Where(c => c.ListId == listId).OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

            var moviesQuery = db.Movies.Where(m => m.ListId == listId && !m.IsDeleted);

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

            vm.Movies = await moviesQuery.OrderBy(m => m.Title)
                .Select(m => new FilterMoviesVm.MovieRow
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
