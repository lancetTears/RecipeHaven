using FinalProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Data;

namespace FinalProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        public IActionResult Index(string tab = "Dashboard")
        {
            if (!IsAdmin())
            {
                TempData["LoginErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ActiveTab = tab;
            return View();
        }

        [HttpGet]
        public IActionResult GetDashboardData()
        {
            if (!IsAdmin())
                return Unauthorized();

            var totalRecipes = _context.Recipes.Count();
            var pendingRecipes = _context.Recipes.Count(r => r.Status == "Pending");
            var totalUsers = _context.Users.Count();
            var activeUsers = _context.Users.Count(u => u.Status == "Active");
            var totalComments = _context.Comments.Count();
            // Calculate average rating 
            var avgRating = _context.RecipeRatings.Any() 
                ? _context.RecipeRatings.Average(r => (double?)r.Rating) ?? 0.0 
                : 0.0;

            // Growth trends diri
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var growthData = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);
                
                var recipesCount = _context.Recipes.Count(r => r.CreatedDate >= monthStart && r.CreatedDate < monthEnd);
                var usersCount = _context.Users.Count(u => u.JoinedDate >= monthStart && u.JoinedDate < monthEnd);
                
                growthData.Add(new
                {
                    month = month.ToString("MMM"),
                    recipes = recipesCount,
                    users = usersCount
                });
            }

            // Recipe categories 
            var categoryData = _context.Recipes
                .Include(r => r.Category)
                .GroupBy(r => r.Category.Name)
                .Select(g => new { category = g.Key, count = g.Count() })
                .ToList();

            // Top rated recipes 
            var approvedRecipes = _context.Recipes
                .Include(r => r.Category)
                .Where(r => r.Status == "Approved")
                .ToList();

            var allRatings = _context.RecipeRatings.ToList();
            var ratingGroups = allRatings.GroupBy(r => r.RecipeId).ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

            var topRecipes = approvedRecipes
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    category = r.Category.Name,
                    rating = ratingGroups.ContainsKey(r.Id) ? ratingGroups[r.Id] : 0.0,
                    likes = r.LikesCount
                })
                .OrderByDescending(r => r.rating)
                .ThenByDescending(r => r.likes)
                .Take(3)
                .ToList();

            return Json(new
            {
                totalRecipes,
                pendingRecipes,
                totalUsers,
                activeUsers,
                totalComments,
                averageRating = Math.Round(avgRating, 1),
                growthData,
                categoryData,
                topRecipes
            });
        }

        [HttpGet]
        public IActionResult GetRecipes()
        {
            if (!IsAdmin())
                return Unauthorized();

            var pendingRecipes = _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Author)
                .Where(r => r.Status == "Pending")
                .Select(r => new
                {
                    id = r.Id,
                    title = r.Name,
                    author = r.Author != null ? r.Author.Name : "Unknown",
                    category = r.Category.Name,
                    created = r.CreatedDate.ToString("yyyy-MM-dd")
                })
                .ToList();

            var allRecipesData = _context.Recipes
                .Include(r => r.Category)
                .ToList();

            var allRatingsForRecipes = _context.RecipeRatings.ToList();
            var ratingGroupsForRecipes = allRatingsForRecipes.GroupBy(r => r.RecipeId).ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

            var allRecipes = allRecipesData.Select(r => new
            {
                id = r.Id,
                title = r.Name,
                category = r.Category.Name,
                status = r.Status,
                rating = ratingGroupsForRecipes.ContainsKey(r.Id) ? ratingGroupsForRecipes[r.Id] : 0.0,
                likes = r.LikesCount,
                isFeatured = r.IsFeatured
            })
            .ToList();

            return Json(new { pendingRecipes, allRecipes });
        }

        [HttpPost]
        public IActionResult ApproveRecipe(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var recipe = _context.Recipes.Find(id);
            if (recipe == null)
                return NotFound();

            recipe.Status = "Approved";
            _context.SaveChanges();

            return Ok(new { message = "Recipe approved successfully." });
        }

        [HttpPost]
        public IActionResult RejectRecipe(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var recipe = _context.Recipes.Find(id);
            if (recipe == null)
                return NotFound();

            _context.Recipes.Remove(recipe);
            _context.SaveChanges();

            return Ok(new { message = "Recipe rejected and deleted." });
        }

        [HttpPost]
        public IActionResult DeleteRecipe(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var recipe = _context.Recipes.Find(id);
            if (recipe == null)
                return NotFound();

            _context.Recipes.Remove(recipe);
            _context.SaveChanges();

            return Ok(new { message = "Recipe deleted successfully." });
        }

        [HttpPost]
        public IActionResult ToggleFeature(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var recipe = _context.Recipes.Find(id);
            if (recipe == null)
                return NotFound();

            recipe.IsFeatured = !recipe.IsFeatured;
            _context.SaveChanges();

            return Ok(new { isFeatured = recipe.IsFeatured });
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            if (!IsAdmin())
                return Unauthorized();

            var users = _context.Users
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    status = u.Status,
                    joined = u.JoinedDate.ToString("yyyy-MM-dd"),
                    recipesCount = _context.Recipes.Count(r => r.AuthorId == u.Id),
                    commentsCount = _context.Comments.Count(c => c.UserId == u.Id)
                })
                .ToList();

            var activeUsers = users.Count(u => u.status == "Active");
            var suspendedUsers = users.Count(u => u.status == "Suspended");
            var totalUsers = users.Count;

            return Json(new { users, activeUsers, suspendedUsers, totalUsers });
        }

        [HttpPost]
        public IActionResult SuspendUser(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            user.Status = "Suspended";
            _context.SaveChanges();

            return Ok(new { message = "User suspended successfully." });
        }

        [HttpPost]
        public IActionResult ActivateUser(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            user.Status = "Active";
            _context.SaveChanges();

            return Ok(new { message = "User activated successfully." });
        }

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();

            return Ok(new { message = "User deleted successfully." });
        }

        [HttpGet]
        public IActionResult GetComments()
        {
            if (!IsAdmin())
                return Unauthorized();

            var comments = _context.Comments
                .Include(c => c.Recipe)
                .Include(c => c.User)
                .OrderByDescending(c => c.DatePosted)
                .Select(c => new
                {
                    id = c.Id,
                    author = c.UserName,
                    content = c.Content,
                    recipe = c.Recipe.Name,
                    recipeId = c.Recipe.Id,
                    date = c.DatePosted.ToString("yyyy-MM-dd")
                })
                .ToList();

            return Json(comments);
        }

        [HttpPost]
        public IActionResult DeleteComment(int id)
        {
            if (!IsAdmin())
                return Unauthorized();

            var comment = _context.Comments.Find(id);
            if (comment == null)
                return NotFound();

            _context.Comments.Remove(comment);
            _context.SaveChanges();

            return Ok(new { message = "Comment deleted successfully." });
        }
    }
}


