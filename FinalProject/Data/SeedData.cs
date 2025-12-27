using FinalProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods; 

namespace FinalProject.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                var dessertCategory = context.Categories.FirstOrDefault(c => c.Name == "Dessert");
                var mainCourseCategory = context.Categories.FirstOrDefault(c => c.Name == "Main Course");
                var mexicanCourseCategory = context.Categories.FirstOrDefault(c => c.Name == "Mexican");
                var pastaCourseCategory = context.Categories.FirstOrDefault(c => c.Name == "PastaNoodles");

                //Check ang data in database already exist
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { Name = "Dessert" },
                        new Category { Name = "Main Course" },
                        new Category { Name = "Appetizer" },
                        new Category { Name = "Vegetarian" },
                        new Category { Name = "Mexican" },
                        new Category { Name = "PastaNoodles" },
                        new Category { Name = "Pasta" },
                        new Category { Name = "Pizza" },
                        new Category { Name = "Salad" }
                        );
                    context.SaveChanges(); 
                }

                // Create admin user if it doesn't exist
                if (!context.Users.Any(u => u.Role == "Admin"))
                {
                    var passwordHasher = new PasswordHasher<User>();
                    var adminUser = new User
                    {
                        Name = "Admin User",
                        Email = "admin@recipehaven.com",
                        Role = "Admin",
                        Status = "Active",
                        JoinedDate = DateTime.Now
                    };
                    adminUser.Password = passwordHasher.HashPassword(adminUser, "Admin123!");
                    context.Users.Add(adminUser);
                    context.SaveChanges();
                }
               
                // Reload categories to ensure they exist
                dessertCategory = context.Categories.FirstOrDefault(c => c.Name == "Dessert");
                mainCourseCategory = context.Categories.FirstOrDefault(c => c.Name == "Main Course");
                var pastaCategory = context.Categories.FirstOrDefault(c => c.Name == "Pasta" || c.Name == "PastaNoodles");
                var mexicanCategory = context.Categories.FirstOrDefault(c => c.Name == "Mexican");
                var saladCategory = context.Categories.FirstOrDefault(c => c.Name == "Salad");
                var vegetarianCategory = context.Categories.FirstOrDefault(c => c.Name == "Vegetarian");
                var pizzaCategory = context.Categories.FirstOrDefault(c => c.Name == "Pizza");
                
                if (dessertCategory == null || mainCourseCategory == null)
                {
                    return; // Categories not seeded yet
                }

                // Update all existing recipes to Approved status if they're not already
                var existingRecipes = context.Recipes.Where(r => r.Status != "Approved").ToList();
                foreach (var recipe in existingRecipes)
                {
                    recipe.Status = "Approved";
                }
                context.SaveChanges();

                // Check if we need to seed recipes (only if key recipes are missing)
                var existingRecipeNames = context.Recipes.Select(r => r.Name).ToList();
                var recipesToAdd = new List<Recipe>();
                
                if (!existingRecipeNames.Contains("Decadent Chocolate Cake"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Decadent Chocolate Cake",
                            Description = "Moist, rich chocolate cake with velvety ganache frosting.Every bite melts in your mouth with deep cocoa flavor and a perfectly tender crumb.",
                            PrepTime = 20,
                            CookTime = 35,
                            Servings = 12,
                            CategoryId = dessertCategory.Id,
                            ImageUrl = "images/DecadentCake.jpg",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "2 cups all-purpose flour",
                                "2 cups sugar",
                                "3/4 cup cocoa powder",
                                "2 tsp baking soda",
                                "1 tsp baking powder",
                                "1 tsp salt",
                                "2 eggs",
                                "1 cup buttermilk",
                                "1 cup hot coffee",
                                "1/2 cup vegetable oil",
                                "2 tsp vanilla extract",
                                "300g dark chocolate for ganache",
                                "1 cup heavy cream for ganache"
                            },
                            Instructions = new List<string>
                            {
                                "Preheat oven to 350°F (175°C). Grease and flour two 9-inch round cake pans.",
                                "In a large bowl, combine flour, sugar, cocoa, baking soda, baking powder, and salt.",
                                "Add eggs, buttermilk, coffee, oil, and vanilla. Beat on medium speed for 2 minutes.",
                                "Pour batter evenly into prepared pans.",
                                "Bake for 30-35 minutes or until a toothpick comes out clean.",
                                "Cool in pans for 10 minutes, then remove to wire racks to cool completely.",
                                "For ganache: heat cream until simmering, pour over chopped chocolate, let sit 5 minutes, then stir until smooth.",
                                "Let ganache cool to spreading consistency, then frost the cooled cake."
                            }
                        });
                }

                if (!existingRecipeNames.Contains("Margherita Pizza"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Margherita Pizza",
                            Description = "Classic Italian pizza with a crispy crust, tangy tomato sauce, fresh mozzarella, and aromatic basil. Simple ingredients that create pizza perfection.",
                            PrepTime = 15,
                            CookTime = 15,
                            Servings = 2,
                            CategoryId = pizzaCategory?.Id ?? mainCourseCategory.Id,
                            ImageUrl = "images/MargheritaPizza.jpg",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "500g pizza dough",
                                "1 cup tomato sauce",
                                "250g fresh mozzarella, sliced",
                                "Fresh basil leaves",
                                "2 tbsp olive oil",
                                "2 cloves garlic, minced",
                                "Salt to taste",
                                "Parmesan cheese for finishing"
                            },
                            Instructions = new List<string>
                            {
                                "Preheat oven to 475°F (245°C) with a pizza stone inside if you have one.",
                                "Roll out pizza dough on a floured surface to desired thickness.",
                                "Mix tomato sauce with minced garlic and a pinch of salt.",
                                "Spread sauce evenly over dough, leaving a 1-inch border.",
                                "Arrange mozzarella slices on top.",
                                "Drizzle with olive oil.",
                                "Bake for 12-15 minutes until crust is golden and cheese is bubbly.",
                                "Remove from oven and immediately top with fresh basil leaves and a sprinkle of Parmesan.",
                                "Let cool for 2 minutes, slice, and serve."
                            }
                        });
                }

                if (!existingRecipeNames.Contains("Classic Carbonara"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Classic Carbonara",
                            Description = "Creamy Italian pasta dish with crispy pancetta, eggs, and Parmesan cheese. A timeless Roman classic that's rich, satisfying, and surprisingly simple to make.",
                            PrepTime = 10,
                            CookTime = 15,
                            Servings = 4,
                            CategoryId = pastaCategory?.Id ?? mainCourseCategory.Id,
                            ImageUrl = "images/ClassicCarbonara.jfif",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "400g spaghetti",
                                "200g pancetta or bacon, diced",
                                "4 large eggs",
                                "1 cup grated Parmesan cheese",
                                "2 cloves garlic, minced",
                                "Freshly ground black pepper",
                                "Salt to taste",
                                "2 tbsp olive oil"
                            },
                            Instructions = new List<string>
                            {
                                "Bring a large pot of salted water to a boil and cook spaghetti according to package directions.",
                                "While pasta cooks, heat olive oil in a large pan over medium heat.",
                                "Add diced pancetta and cook until crispy, about 5-7 minutes.",
                                "Add minced garlic and cook for 1 minute more.",
                                "In a bowl, whisk together eggs, Parmesan cheese, and black pepper.",
                                "Drain pasta, reserving 1 cup of pasta water.",
                                "Immediately add hot pasta to the pan with pancetta, remove from heat.",
                                "Quickly pour egg mixture over pasta, tossing constantly. The heat from the pasta will cook the eggs.",
                                "Add pasta water a little at a time until sauce is creamy.",
                                "Serve immediately with extra Parmesan and black pepper."
                            }
                        });
                }

                if (!existingRecipeNames.Contains("Beef Tacos"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Beef Tacos",
                            Description = "Seasoned ground beef nestled in warm tortillas with fresh toppings. A crowd-pleasing favorite with bold spices and endless customization options.",
                            PrepTime = 15,
                            CookTime = 20,
                            Servings = 4,
                            CategoryId = mexicanCategory?.Id ?? mainCourseCategory.Id,
                            ImageUrl = "images/BeefTacos.jfif",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "1 lb ground beef",
                                "8 small corn or flour tortillas",
                                "1 onion, diced",
                                "2 cloves garlic, minced",
                                "1 tbsp chili powder",
                                "1 tsp cumin",
                                "1 tsp paprika",
                                "1/2 tsp oregano",
                                "Salt and pepper to taste",
                                "Shredded lettuce",
                                "Diced tomatoes",
                                "Shredded cheese",
                                "Sour cream",
                                "Salsa"
                            },
                            Instructions = new List<string>
                            {
                                "Heat a large skillet over medium-high heat.",
                                "Add ground beef and cook until browned, breaking it up with a spoon.",
                                "Add diced onion and cook until softened, about 5 minutes.",
                                "Add garlic and cook for 1 minute.",
                                "Stir in chili powder, cumin, paprika, oregano, salt, and pepper.",
                                "Cook for 2-3 minutes until spices are fragrant.",
                                "Warm tortillas in a dry pan or microwave.",
                                "Fill each tortilla with beef mixture.",
                                "Top with lettuce, tomatoes, cheese, sour cream, and salsa.",
                                "Serve immediately."
                            }
                        });
                }

                if (!existingRecipeNames.Contains("Grilled Chicken Caesar Salad"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Grilled Chicken Caesar Salad",
                            Description = "Crisp romaine lettuce with tender grilled chicken, crunchy croutons, and creamy Caesar dressing. A classic salad that's hearty enough to be a meal.",
                            PrepTime = 20,
                            CookTime = 15,
                            Servings = 4,
                            CategoryId = saladCategory?.Id ?? mainCourseCategory.Id,
                            ImageUrl = "images/GrilledChickenCaesarSalad.jfif",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "2 boneless, skinless chicken breasts",
                                "1 large head romaine lettuce, chopped",
                                "1/2 cup Caesar dressing",
                                "1/2 cup croutons",
                                "1/4 cup grated Parmesan cheese",
                                "2 tbsp olive oil",
                                "Salt and pepper to taste",
                                "Lemon wedges for serving"
                            },
                            Instructions = new List<string>
                            {
                                "Preheat grill or grill pan to medium-high heat.",
                                "Season chicken breasts with salt, pepper, and olive oil.",
                                "Grill chicken for 6-7 minutes per side until cooked through.",
                                "Let chicken rest for 5 minutes, then slice.",
                                "In a large bowl, combine chopped romaine lettuce with Caesar dressing.",
                                "Toss to coat evenly.",
                                "Divide salad among plates.",
                                "Top with sliced grilled chicken.",
                                "Sprinkle with croutons and Parmesan cheese.",
                                "Serve with lemon wedges."
                            }
                        });
                }

                if (!existingRecipeNames.Contains("Vegetable Stir Fry"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Vegetable Stir Fry",
                            Description = "Colorful mix of fresh vegetables stir-fried to perfection with a savory sauce. Quick, healthy, and packed with flavor and nutrients.",
                            PrepTime = 15,
                            CookTime = 10,
                            Servings = 4,
                            CategoryId = vegetarianCategory?.Id ?? mainCourseCategory.Id,
                            ImageUrl = "images/VegetableStirFry.jfif",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "2 cups broccoli florets",
                                "1 bell pepper, sliced",
                                "1 carrot, julienned",
                                "1 cup snap peas",
                                "1 cup mushrooms, sliced",
                                "2 cloves garlic, minced",
                                "1 inch ginger, grated",
                                "3 tbsp soy sauce",
                                "1 tbsp sesame oil",
                                "1 tbsp cornstarch",
                                "2 tbsp vegetable oil",
                                "2 green onions, chopped"
                            },
                            Instructions = new List<string>
                            {
                                "Heat vegetable oil in a large wok or skillet over high heat.",
                                "Add garlic and ginger, stir-fry for 30 seconds.",
                                "Add carrots and stir-fry for 2 minutes.",
                                "Add broccoli and bell pepper, stir-fry for 3 minutes.",
                                "Add snap peas and mushrooms, stir-fry for 2 minutes.",
                                "In a small bowl, mix soy sauce, sesame oil, and cornstarch.",
                                "Pour sauce over vegetables and stir-fry for 1 minute until thickened.",
                                "Garnish with green onions.",
                                "Serve hot over rice or noodles."
                            }
                        });
                }

                if (!existingRecipeNames.Contains("Homemade Lasagna"))
                {
                    recipesToAdd.Add(
                        new Recipe
                        {
                            Name = "Homemade Lasagna",
                            Description = "Layers of pasta, rich meat sauce, creamy ricotta, and melted mozzarella. A comforting Italian classic that's perfect for feeding a crowd.",
                            PrepTime = 30,
                            CookTime = 45,
                            Servings = 8,
                            CategoryId = pastaCategory?.Id ?? mainCourseCategory.Id,
                            ImageUrl = "images/HomeMadeMeal.jfif",
                            Rating = 5,
                            LikesCount = 0,
                            IsLikedByUser = false,
                            Status = "Approved",
                            CreatedDate = DateTime.Now.AddMonths(-2),
                            Ingredients = new List<string>
                            {
                                "12 lasagna noodles",
                                "1 lb ground beef",
                                "1 lb Italian sausage",
                                "1 onion, diced",
                                "3 cloves garlic, minced",
                                "24 oz marinara sauce",
                                "15 oz ricotta cheese",
                                "2 cups shredded mozzarella",
                                "1/2 cup grated Parmesan",
                                "1 egg",
                                "2 tbsp fresh basil, chopped",
                                "Salt and pepper to taste"
                            },
                            Instructions = new List<string>
                            {
                                "Preheat oven to 375°F (190°C).",
                                "Cook lasagna noodles according to package directions, then drain.",
                                "In a large skillet, brown ground beef and sausage over medium heat.",
                                "Add onion and cook until softened, about 5 minutes.",
                                "Add garlic and cook for 1 minute.",
                                "Stir in marinara sauce and simmer for 10 minutes.",
                                "In a bowl, mix ricotta, egg, Parmesan, basil, salt, and pepper.",
                                "In a 9x13 baking dish, layer: sauce, noodles, ricotta mixture, mozzarella.",
                                "Repeat layers, ending with mozzarella on top.",
                                "Cover with foil and bake for 25 minutes.",
                                "Remove foil and bake for 15 more minutes until bubbly.",
                                "Let rest for 10 minutes before serving."
                            }
                        });
                }

                if (recipesToAdd.Any())
                {
                    context.Recipes.AddRange(recipesToAdd);
                    context.SaveChanges();
                }
            }

        }

    }
}
