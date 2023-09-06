using VinderenApi.Enums;
using VinderenApi.Models.Interfaces;

namespace VinderenApi.Models
{
    public class TaekwondoModel : IProfile
    {
        public int Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Email { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string FirstName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string LastName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime DateOfBirth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Discipline Discipline { get; set; } = Discipline.Taekwondo;
        public string Proficiency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime DateOfCreation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        
    }
}
