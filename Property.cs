using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate
{
    public abstract class Property
    {
        public string Address { get; set; }
        public double PropertyValue { get; set; }
        public double IndoorArea { get; set; }
        public Property(string address, double price, double area)
        {
            Address = address;
            PropertyValue = price;
            IndoorArea = area;
        }
        
        public override string ToString()
        {
            return ($"Address: {Address}, Price: {PropertyValue}, Area: {IndoorArea}");
        }
    }
    
}
// banking system
//healthcare system
//account token generation
//online site management
//file format conversion
//booking system
//chat application - cu windows form
//real estate agency
//stock exchange demo account
//restaurant table keeper
//game design