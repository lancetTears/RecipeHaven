namespace FinalProject.Models
{
    public class UserAndRecipesViewModel
    {
        public User User { get; set; }
        public List<Recipe> Recipes { get; set; }

        public List<Category> Categories { get; set; }
        public Recipe Recipe { get; set; }

        public int LikesCount { get; set; }
        public bool IsLikedByUser { get; set; }
    }
}
