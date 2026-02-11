using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieBox.Models;
using MovieBox.ViewModels;

namespace MovieBox.Controllers;

public class ManageController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var vm = new ListVm();
        await HydrateLists(vm);
        
        return View(vm);
    }
    
    [HttpGet]
    public async Task<IActionResult> EditList(int id)
    {
        var list = await db.Lists.FindAsync(id);
        if (list is null) return NotFound();

        var vm = new ListVm
        {
            EditId = list.Id,
            Load = new LoadList { Name = list.Name }
        };
        await HydrateLists(vm);

        return View("List", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveList(ListVm vm)
    {
        if (!ModelState.IsValid) return View("List", await HydrateLists(vm));

        var normalized = vm.Load.Name.Trim();

        if (vm.EditId == null)
        {
            var exists = await db.Lists.AnyAsync(l => l.Name == normalized);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.Load.Name), "A list with that name already exists.");
                return View("List", await HydrateLists(vm));
            }

            db.Lists.Add(new List { Name = normalized });
            await db.SaveChangesAsync();

            TempData["Success"] = "List created!";
        }
        else
        {
            if (vm.EditId is null) return BadRequest();

            var list = await db.Lists.FindAsync(vm.EditId.Value);
            if (list is null) return NotFound();

            var exists = await db.Lists.AnyAsync(l => l.Id != list.Id && l.Name == normalized);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.Load.Name), "A list with that name already exists.");
                return View("List", await HydrateLists(vm));
            }

            list.Name = normalized;
            await db.SaveChangesAsync();

            TempData["Success"] = "List updated!";
        }

        return RedirectToAction(nameof(List));

    }
    
    public async Task<ListVm> HydrateLists(ListVm x)
    {
        x.Lists = await db.Lists.OrderBy(l => l.Name)
            .Select(l => new ListVm.ListRow { Id = l.Id, Title = l.Name }).ToListAsync();
        return x;
    }

}