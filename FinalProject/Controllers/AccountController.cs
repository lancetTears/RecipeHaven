using Microsoft.AspNetCore.Mvc;
using FinalProject.Data;  
using FinalProject.Models; 
using Microsoft.AspNetCore.Identity; 


namespace FinalProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

       
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {

            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                ViewBag.OpenRegisterModal = true;
                return RedirectToAction("Index", "Home");
            }

            if (user.Password != user.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password must match.");
                ViewBag.OpenRegisterModal = true;
                return RedirectToAction("Index", "Home");
            }


            if (ModelState.IsValid)
            {
                var passwordHasher = new PasswordHasher<User>();
                user.Password = passwordHasher.HashPassword(user, user.Password);

                user.Role = "User";
                user.Status = "Active";
                user.JoinedDate = DateTime.Now;

                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your account has been registered successfully!";

                return RedirectToAction("Index", "Home");
            }

           
            ViewBag.OpenRegisterModal = true;
            return RedirectToAction("Index", "Home");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password, string returnUrl = null)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                TempData["OpenLoginModal"] = true;
                TempData["LoginErrorMessage"] = "The email doesn't exist.";
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            // Check if user is suspended
            if (user.Status == "Suspended")
            {
                TempData["OpenLoginModal"] = true;
                TempData["LoginErrorMessage"] = "Your account has been suspended. Please contact an administrator.";
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                TempData["OpenLoginModal"] = true;
                TempData["LoginErrorMessage"] = "Invalid password.";
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.SetString("Username", user.Name);
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);

            TempData["LoginSuccess"] = $"Welcome, {user.Name}!";
            
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
        
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                TempData["LoginErrorMessage"] = "You need to be logged in to browse recipes.";
                return RedirectToAction("Index", "Home");  
            }

            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserRole");

            TempData["SuccessMessage"] = "You have been logged out successfully!";
            return RedirectToAction("Index", "Home");
        }


    }
}


