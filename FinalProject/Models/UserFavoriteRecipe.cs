namespace FinalProject.Models
{
    public class UserFavoriteRecipe
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
    }
}
