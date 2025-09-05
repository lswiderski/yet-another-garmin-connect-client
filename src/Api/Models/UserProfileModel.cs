namespace Api.Models
{
    public class UserProfileModel
    {
        public int Age { set; get; }
        public int Height { set; get; }
        public GenderEnum Gender { set; get; } = GenderEnum.Male;
    }
}
