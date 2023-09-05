using VinderenApi.Enums;

namespace VinderenApi.Models.Interfaces
{
    public interface IProfile
    {
        int Id { get; set; }
        string Name { get; set; }
        Discipline Discipline { get; set; }
        //proficiency...
        
    }
}
