using System;

namespace RealEstate
{
    public class RentableApartment : Apartment, IRentable
    {
        public bool IsRented { get; set; }
        public double MonthlyRent { get; set; }

        public RentableApartment(string address, double price, double area, int floor, int bedrooms, bool hasElevator, bool isRented, double monthlyRent)
            : base(address, price, area, floor, bedrooms, hasElevator)
        {
            IsRented = isRented;
            MonthlyRent = monthlyRent;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, IsRented: {IsRented}, MonthlyRent: {MonthlyRent}";
        }
    }
}