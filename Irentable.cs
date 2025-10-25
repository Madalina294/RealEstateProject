using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate
{
    public interface IRentable
    {
        bool IsRented { get; set; }
        double MonthlyRent { get; set; }
    }
}
