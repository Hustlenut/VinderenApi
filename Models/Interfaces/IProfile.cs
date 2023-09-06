using VinderenApi.Enums;

namespace VinderenApi.Models.Interfaces
{
    public interface IProfile
    {
        int Id { get; set; }
        string Email { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        DateTime DateOfBirth { get; set; }
        Discipline Discipline { get; set; }
        string Proficiency { get; set; }
        string Description { get; set; }
        DateTime DateOfCreation { get; set; }
        
    }
}
