using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate
{
    public class House : Property
    {
        public int Floors { get; set; }
        public bool HasGarden { get; set; }
        public double OutDoorArea { get; set; }

        // map cu numele camerei si suprafata ei

        // Computed read-only property that sums inherited IndoorArea and this OutDoorArea.
        public double TotalArea => IndoorArea + OutDoorArea;
       
        public House(string address, double price, double inDoorArea, double outDoorArea, int floors, bool hasGarden)
            : base(address, price, inDoorArea + outDoorArea)
        {
            Floors = floors;
            HasGarden = hasGarden;
            OutDoorArea = outDoorArea;
        }
        public override string ToString()
        {
            return $"House - {base.ToString()}, Floors: {Floors}, HasGarden: {HasGarden}, TotalArea: {TotalArea}";
        }
    }
}
