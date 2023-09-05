using System.Globalization;
using VinderenApi.Discipline.Interface;

namespace VinderenApi.Discipline
{
    public class TKDgrade : ILevel
    {
        public int Level(int level)
        {
            return level;
        }

        private int White { get; set; } = 10;
        private int Yellow_stripe { get; set; } = 9;
        private int Yellow { get; set; } = 8;
        private int Green_stripe { get; set; } = 7;
        private int Green { get; set; } = 6;
        private int Blue_stripe { get; set; } = 5;
        private int Blue { get; set; } = 4;
        private int Red_stripe { get; set; } = 3;
        private int Red { get; set; } = 2;
        private int Black_stripe { get; set; } = 1;
        private int First_dan { get; set; } = -1;
        private int Second_dan { get; set; } = -2;
        private int Third_dan { get; set; } = -3;
        private int Fourth_dan { get; set; } = -4;
        private int Fifth_dan { get; set; } = -5;
        private int Sixth_dan { get; set; } = -6;
        private int Seventh_dan { get; set; } = -7;
        private int Eight_dan { get; set; } = -8;
        private int Ninth_dan { get; set; } = -9;

    }
}
