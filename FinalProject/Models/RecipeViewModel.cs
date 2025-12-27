using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace FinalProject.Models
{
    public class RecipeViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int Servings { get; set; }

        public string PrepTime { get; set; }
        public string CookTime { get; set; }

        public IFormFile ImageFile { get; set; } 

        public List<string> Ingredients { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();
    }
}
