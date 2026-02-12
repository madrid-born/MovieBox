using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieBox.Models;
using MovieBox.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MovieBox.Controllers;

public class ManageController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    
    #region List

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
    
    #endregion

    #region Category
    
        [HttpGet]
        public async Task<IActionResult> Category()
        {
            var vm = new CategoryVm();
            await HydrateListsForCategory(vm);
            await HydrateCategories(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var cat = await db.Categories.FindAsync(id);
            if (cat is null) return NotFound();

            var vm = new CategoryVm
            {
                EditId = cat.Id,
                Load = new LoadCategory { Name = cat.Name, ListId = cat.ListId }
            };

            await HydrateListsForCategory(vm);
            await HydrateCategories(vm);

            return View("Category", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(CategoryVm vm)
        {
            await HydrateListsForCategory(vm);

            if (!ModelState.IsValid)
            {
                await HydrateCategories(vm);
                return View("Category", vm);
            }

            var normalized = vm.Load.Name.Trim();
            var listId = vm.Load.ListId!.Value;

            if (vm.EditId is null)
            {
                var exists = await db.Categories.AnyAsync(c => c.ListId == listId && c.Name == normalized);
                if (exists)
                {
                    ModelState.AddModelError(nameof(vm.Load.Name), "A category with that name already exists in this list.");
                    await HydrateCategories(vm);
                    return View("Category", vm);
                }

                db.Categories.Add(new Category { Name = normalized, ListId = listId });
                await db.SaveChangesAsync();
                TempData["Success"] = "Category created!";
            }
            else
            {
                var cat = await db.Categories.FindAsync(vm.EditId.Value);
                if (cat is null) return NotFound();

                var exists = await db.Categories.AnyAsync(c => c.Id != cat.Id && c.ListId == listId && c.Name == normalized);

                if (exists)
                {
                    ModelState.AddModelError(nameof(vm.Load.Name), "A category with that name already exists in this list.");
                    await HydrateCategories(vm);
                    return View("Category", vm);
                }

                cat.Name = normalized;
                cat.ListId = listId;
                await db.SaveChangesAsync();
                TempData["Success"] = "Category updated!";
            }

            return RedirectToAction(nameof(Category));
        }

        private async Task HydrateListsForCategory(CategoryVm vm)
        {
            vm.Lists = await db.Lists.OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name }).ToListAsync();
        }

        private async Task HydrateCategories(CategoryVm vm)
        {
            vm.Categories = await db.Categories.OrderBy(c => c.Name).Select(c => new CategoryVm.CategoryRow
                { Id = c.Id, Title = c.Name, ListId = c.ListId, ListTitle = c.List.Name }).ToListAsync();
        }
    
    #endregion
}