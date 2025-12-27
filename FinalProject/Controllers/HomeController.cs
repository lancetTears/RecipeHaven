    using FinalProject.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Diagnostics;
    using FinalProject.Data;
    using System.Text.Json;

namespace FinalProject.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index(int? categoryId, string ingredient, string recipeName)
    {
        //var user = _context.Users.FirstOrDefault(); 
        var recipes = _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.Status == "Approved")
            .ToList();

        var username = HttpContext.Session.GetString("Username");
        User? user = null;

        if (!string.IsNullOrEmpty(username))
        {
            user = _context.Users
                           .Include(u => u.FavoriteRecipes)
                           .FirstOrDefault(u => u.Name == username);
        }

        if (categoryId.HasValue && categoryId.Value != 0)
        {
            recipes = recipes.Where(r => r.CategoryId == categoryId.Value).ToList();
        }

        if (!string.IsNullOrEmpty(ingredient))
        {

            var searchIngredients = ingredient
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .ToList();
            recipes = recipes.Where(r => searchIngredients
                        .All(si => r.Ingredients
                        .Any(ri => ri.Contains(si, StringComparison.OrdinalIgnoreCase))))
                        .ToList();
        }

        if (!string.IsNullOrEmpty(recipeName))
        {
            recipes = recipes
                .Where(r => r.Name.Contains(recipeName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Separate featured recipes 
        var featuredRecipes = new List<Recipe>();
        var isFiltered = categoryId.HasValue && categoryId.Value != 0 || 
                        !string.IsNullOrEmpty(ingredient) || 
                        !string.IsNullOrEmpty(recipeName);
        
        if (!isFiltered)
        {
            featuredRecipes = _context.Recipes
                .Include(r => r.Category)
                .Where(r => r.Status == "Approved" && r.IsFeatured)
                .OrderByDescending(r => r.CreatedDate)
                .Take(6) 
                .ToList();
        }

        // Exclude featured recipes from main list
        
        if (!isFiltered && featuredRecipes.Any())
        {
            var featuredRecipeIds = featuredRecipes.Select(r => r.Id).ToList();
            recipes = recipes
                .Where(r => !featuredRecipeIds.Contains(r.Id)) 
                .OrderByDescending(r => r.CreatedDate)
                .ToList();
        }
        else
        {
            
            recipes = recipes
                .OrderByDescending(r => r.IsFeatured) 
                .ThenByDescending(r => r.CreatedDate)
                .ToList();
        }

        var categories = _context.Categories.ToList();


        var model = new UserAndRecipesViewModel
        {
            User = user,
            Recipes = recipes,
            Categories = categories
        };

        ViewBag.SelectedCategoryId = categoryId ?? 0;
        ViewBag.SearchIngredient = ingredient ?? "";
        ViewBag.SearchRecipeName = recipeName ?? "";
        ViewBag.FeaturedRecipes = featuredRecipes;
        ViewBag.IsFiltered = isFiltered;

        return View(model);
    }


    //public IActionResult Privacy()
    //{
    //    return View();
    //}

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult Browse()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            TempData["LoginErrorMessage"] = "You need to be logged in to browse recipes.";
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    public IActionResult Recipe_Details(int id)
    {
        var recipe = _context.Recipes
            .Include(r => r.Category)
            .Include(r => r.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefault(r => r.Id == id);

        if (recipe == null)
        {
            return NotFound();
        }

        var username = HttpContext.Session.GetString("Username");
        User? user = null;

        if (!string.IsNullOrEmpty(username))
        {
            user = _context.Users
                           .Include(u => u.FavoriteRecipes)
                           .FirstOrDefault(u => u.Name == username);

            recipe.IsLikedByUser = _context.UserLikedRecipes
                                      .Any(l => l.UserId == user.Id && l.RecipeId == recipe.Id);
        }

        // Calculate average rating 
        var ratings = _context.RecipeRatings.Where(r => r.RecipeId == recipe.Id).ToList();
        if (ratings.Any())
        {
            recipe.AverageRating = Math.Round(ratings.Average(r => r.Rating), 1);
        }
        else
        {
            recipe.AverageRating = 0.0;
        }

        
        var recipeInDb = _context.Recipes.Find(recipe.Id);
        if (recipeInDb != null)
        {
            recipeInDb.AverageRating = recipe.AverageRating;
            _context.SaveChanges();
        }

        ViewBag.RatingCount = ratings.Count();

        var model = new UserAndRecipesViewModel
        {
            Recipe = recipe,
            User = user
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult UploadRecipe()
    {
        var categories = _context.Categories.ToList();
        ViewBag.Categories = categories;

        return View(new RecipeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UploadRecipe(RecipeViewModel model)
    {
        _logger.LogInformation("UploadRecipe POST hit");

        if (ModelState.IsValid)
        {
            _logger.LogInformation("ModelState is valid");

            var username = HttpContext.Session.GetString("Username");
            var user = _context.Users.FirstOrDefault(u => u.Name == username);
            
            if (user == null)
            {
                TempData["LoginErrorMessage"] = "You need to be logged in to upload recipes.";
                return RedirectToAction("Index", "Home");
            }

            var recipe = new Recipe
            {

                Name = model.Name,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Servings = model.Servings,
                PrepTime = int.TryParse(model.PrepTime?.Replace(" min", ""), out var prep) ? prep : 0,
                CookTime = int.TryParse(model.CookTime?.Replace(" min", ""), out var cook) ? cook : 0,
                Ingredients = (Request.Form["Ingredients"].ToString() ?? "")
                    .Split("|||", StringSplitOptions.RemoveEmptyEntries)
                    .ToList(),

                Instructions = (Request.Form["Instructions"].ToString() ?? "")
                    .Split("|||", StringSplitOptions.RemoveEmptyEntries)
                    .ToList(),
                LikesCount = 0,
                IsLikedByUser = false,
                Status = "Pending",
                AuthorId = user.Id,
                CreatedDate = DateTime.Now
            };


            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileName = Path.GetFileName(model.ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }
                recipe.ImageUrl = "/images/" + fileName;
            }

            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Recipe successfully added!";
            return RedirectToAction("UploadRecipe");
        }
        else
        {
            _logger.LogWarning("ModelState is invalid. Errors:");

            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    _logger.LogWarning($"{state.Key}: {error.ErrorMessage}");
                }
            }
        }

        ViewBag.Categories = _context.Categories.ToList();
        return View(model);
    }

    public IActionResult MyFavorites()
    {

        var username = HttpContext.Session.GetString("Username");

        if (string.IsNullOrEmpty(username))
            return RedirectToAction("Index");

        var user = _context.Users
                           .Include(u => u.FavoriteRecipes)
                           .ThenInclude(f => f.Recipe)
                           .ThenInclude(r => r.Category)
                           .FirstOrDefault(u => u.Name == username);

        if (user == null) return NotFound();

        var favoriteRecipes = user.FavoriteRecipes.Select(f => f.Recipe).ToList();
        var model = new UserAndRecipesViewModel
        {
            User = user,
            Recipes = favoriteRecipes,
            Categories = _context.Categories.ToList()
        };
        return View(model);
    }

    [HttpPost]
    public IActionResult ToggleFavorite([FromBody] FavoriteDto dto)
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var user = _context.Users
                           .Include(u => u.FavoriteRecipes)
                           .FirstOrDefault(u => u.Name == username);

        if (user == null) return NotFound();

        var favorite = user.FavoriteRecipes.FirstOrDefault(f => f.RecipeId == dto.RecipeId);
        if (favorite != null)
        {
            _context.UserFavoriteRecipes.Remove(favorite);
        }
        else
        {
            _context.UserFavoriteRecipes.Add(new UserFavoriteRecipe
            {
                UserId = user.Id,
                RecipeId = dto.RecipeId
            });
        }

        _context.SaveChanges();
        return Ok();
    }

    [HttpPost]
    public IActionResult ToggleLike([FromBody] FavoriteDto dto)
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var user = _context.Users.FirstOrDefault(u => u.Name == username);
        var recipe = _context.Recipes.FirstOrDefault(r => r.Id == dto.RecipeId);

        if (user == null || recipe == null)
            return NotFound();

        var alreadyLiked = _context.UserLikedRecipes
                                   .Any(l => l.UserId == user.Id && l.RecipeId == recipe.Id);

        if (alreadyLiked)
        {
            var likeEntry = _context.UserLikedRecipes
                                    .First(l => l.UserId == user.Id && l.RecipeId == recipe.Id);
            _context.UserLikedRecipes.Remove(likeEntry);
            recipe.LikesCount = recipe.LikesCount > 0 ? recipe.LikesCount - 1 : 0;
        }
        else
        {
            _context.UserLikedRecipes.Add(new UserLikedRecipe
            {
                UserId = user.Id,
                RecipeId = recipe.Id
            });
            recipe.LikesCount++;
        }

        _context.SaveChanges();

        return Ok(new
        {
            likesCount = recipe.LikesCount,
            isLiked = !alreadyLiked
        });
    }
    [HttpPost]
    public IActionResult RateRecipe([FromBody] RatingDto dto)
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { message = "You need to be logged in to rate the recipe." });
        }

        var user = _context.Users.FirstOrDefault(u => u.Name == username);
        var recipe = _context.Recipes.FirstOrDefault(r => r.Id == dto.RecipeId);

        if (user == null || recipe == null)
        {
            return NotFound(new { message = "Recipe or User not found." });
        }

        var existingRating = _context.RecipeRatings
                                     .FirstOrDefault(r => r.UserId == user.Id && r.RecipeId == recipe.Id);

        if (existingRating != null)
        {
            existingRating.Rating = dto.Rating; 
        }
        else
        {
            _context.RecipeRatings.Add(new RecipeRating
            {
                RecipeId = recipe.Id,
                UserId = user.Id,
                Rating = dto.Rating
            });
        }

        try
        {
            _context.SaveChanges(); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while saving the rating.", details = ex.Message });
        }

        var avgRating = _context.RecipeRatings
                                .Where(r => r.RecipeId == recipe.Id)
                                .Average(r => r.Rating);

        recipe.AverageRating = Math.Round(avgRating, 1);
        _context.SaveChanges();

        return Ok(new
        {
            averageRating = recipe.AverageRating
        });
    }

    [HttpPost]
    public IActionResult PostComment([FromBody] CommentDto dto)
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { message = "You need to be logged in to comment." });
        }

        var user = _context.Users.FirstOrDefault(u => u.Name == username);
        var recipe = _context.Recipes.FirstOrDefault(r => r.Id == dto.RecipeId);

        if (user == null || recipe == null)
        {
            return NotFound(new { message = "Recipe or User not found." });
        }

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest(new { message = "Comment cannot be empty." });
        }

        var comment = new Comment
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            UserName = user.Name,
            Content = dto.Content,
            DatePosted = DateTime.Now
        };

        _context.Comments.Add(comment);
        _context.SaveChanges();

        return Ok(new
        {
            id = comment.Id,
            userName = comment.UserName,
            content = comment.Content,
            datePosted = comment.DatePosted.ToString("yyyy-MM-dd")
        });
    }








}
