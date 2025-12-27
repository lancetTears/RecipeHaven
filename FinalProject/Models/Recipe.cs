using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinalProject.Models
{
    public class Recipe
    {
        public int Id { get; set; }  

        [Required]
        public string Name { get; set; } 

        [Required]
        public string Description { get; set; }  

        public int PrepTime { get; set; }  

        public int CookTime { get; set; }  

        public int Servings { get; set; }  

        [Required]
        public int CategoryId { get; set; } 
        public Category Category { get; set; }  

        public string ImageUrl { get; set; }

        public int Rating { get; set; }
        public List<string> Ingredients { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();
        public List<Comment> Comments { get; set; } = new List<Comment>();

        public int LikesCount { get; set; }
        public bool IsLikedByUser { get; set; }

        public double AverageRating { get; set; } = 0.0;
        public List<UserFavoriteRecipe> UsersWhoFavorited { get; set; } = new();
        public string Status { get; set; } = "Pending";
        public int? AuthorId { get; set; }
        public User? Author { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsFeatured { get; set; } = false;
    }

    public class Comment
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public DateTime DatePosted { get; set; } = DateTime.Now;
    }
}
