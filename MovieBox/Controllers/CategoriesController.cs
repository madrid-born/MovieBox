using MovieBox.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using MovieBox.ViewModels;

namespace MovieBox.Controllers;


public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ViewModel (small and local; you can move it to ViewModels folder)
    public class CreateCategoryVM
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    // GET: /Categories/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCategoryVM());
    }

    // POST: /Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCategoryVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var normalized = vm.Name.Trim();

        // Optional: prevent duplicates
        var exists = await _db.Categories.AnyAsync(c => c.Name == normalized);
        if (exists)
        {
            ModelState.AddModelError(nameof(vm.Name), "A category with that name already exists.");
            return View(vm);
        }

        var category = new Category
        {
            Name = normalized
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Category created!";
        return RedirectToAction(nameof(Create));
    }
    
    // GET: /Categories/Items
    // /Categories/Items?categoryId=3
    [HttpGet]
    public async Task<IActionResult> Items(int? categoryId)
    {
        var vm = new CategoryItemsVM
        {
            SelectedCategoryId = categoryId,
            Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync()
        };

        if (categoryId is null)
            return View(vm);

        // Pull movies in that category through the join table
        // Includes List name (since Movie only has ListId in your model)
        vm.Movies = await (
            from ci in _db.CategorizedItems
            join m in _db.Movies on ci.MovieId equals m.Id
            join l in _db.Lists on m.ListId equals l.Id
            where ci.CategoryId == categoryId.Value
            orderby m.Title
            select new CategoryItemsVM.MovieRow
            {
                Id = m.Id,
                Title = m.Title,
                Year = m.Year,
                Length = m.Length,
                Language = m.Language,
                ListName = l.Name,
                IsSeen = m.IsSeen,
                IsAvailable = m.IsAvailable
            }
        ).Distinct().ToListAsync();

        return View(vm);
    }
    
    // GET: /Categories/Intersect
    [HttpGet]
    public async Task<IActionResult> Intersect([FromQuery] List<int> selectedCategoryIds)
    {
        var vm = new CategoryIntersectionVM
        {
            SelectedCategoryIds = selectedCategoryIds ?? new List<int>(),
            Categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync()
        };

        // No selection => no results (or you can decide to show all)
        if (vm.SelectedCategoryIds.Count == 0)
            return View(vm);

        // IMPORTANT: intersection logic:
        // A movie must have categorizedItems for ALL selected category IDs.
        // We do: filter join rows to selected categories, group by MovieId,
        // keep those where distinct category count == selected count.
        var selectedCount = vm.SelectedCategoryIds.Distinct().Count();

        var movieIds = await _db.CategorizedItems
            .Where(ci => vm.SelectedCategoryIds.Contains(ci.CategoryId))
            .GroupBy(ci => ci.MovieId)
            .Where(g => g.Select(x => x.CategoryId).Distinct().Count() == selectedCount)
            .Select(g => g.Key)
            .ToListAsync();

        // Pull movie details (and list name)
        vm.Movies = await (
            from m in _db.Movies
            join l in _db.Lists on m.ListId equals l.Id
            where movieIds.Contains(m.Id)
            orderby m.Title
            select new CategoryIntersectionVM.MovieRow
            {
                Id = m.Id,
                Title = m.Title,
                Year = m.Year,
                Length = m.Length,
                Language = m.Language,
                ListName = l.Name,
                IsSeen = m.IsSeen,
                IsAvailable = m.IsAvailable
            }
        ).ToListAsync();

        return View(vm);
    }
}
