using System;

namespace RealEstate
{
    public class Apartment : Property
    {
        public int Floor { get; set; }
        public int Bedrooms { get; set; }

        public bool HasElevator { get; set; }

        public Apartment(string address, double price, double area, int floor, int bedrooms, bool hasElevator )
            : base(address, price, area)
        {
            Floor = floor;
            Bedrooms = bedrooms;
            HasElevator = hasElevator;
        }

        public override string ToString()
        {
            return $"Apartment - {base.ToString()}, Floor: {Floor}, Bedrooms: {Bedrooms}";
        }
    }
}