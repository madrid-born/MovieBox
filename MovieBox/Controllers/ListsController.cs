using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using MovieBox.Models;

namespace MovieBox.Controllers;

public class ListsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ListsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ViewModel (small and local; you can move it to ViewModels folder)
    public class CreateListVM
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    // GET: /Lists/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateListVM());
    }

    // POST: /Lists/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateListVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var normalized = vm.Name.Trim();

        // Optional: prevent duplicates
        var exists = await _db.Lists.AnyAsync(l => l.Name == normalized);
        if (exists)
        {
            ModelState.AddModelError(nameof(vm.Name), "A list with that name already exists.");
            return View(vm);
        }

        var list = new List
        {
            Name = normalized
        };

        _db.Lists.Add(list);
        await _db.SaveChangesAsync();

        TempData["Success"] = "List created!";
        return RedirectToAction(nameof(Create));
    }
}
