using RealEstate;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate
{
    public  class RealEstateAgency
    {

        private List<Property> Properties { get; set; }

        // Agency capital (sum of values of owned properties). Kept private; read via GetCapital().
        private double Capital { get; set; }

        // Agency cash/budget used for purchases, rent collection, etc.
        private double Budget { get; set; }
       
        public RealEstateAgency()
        {
            Properties = new List<Property>();
            Capital = 0;
            Budget = 0;
        }

        // Recalculate capital from current Properties list (use after loading file)
        // Now returns the recalculated value so it can be used directly.
        public double RecalculateCapital()
        {
            Capital = Properties.Sum(p => p.PropertyValue);
            return Capital;
        }

        // Read-only accessors
        public double GetCapital() => RecalculateCapital();
        public double GetBudget() => Budget;

        // convenience method used by Program.cs when reloading the file
        public void RemoveAllProperties()
        {
            Properties.Clear();
            Capital = 0;
        }

        public Property findPropertyByAddress(string address)
        {
            return Properties.FirstOrDefault(p => p.Address == address);
        }

        public void AddProperty(Property property)
        {
            this.Properties.Add(property);
        }

        public void RemoveProperty(Property property)
        {
            this.Properties.Remove(property); // poti sa stergi si din fisier linia respectiva
        }

        // Purchase a property: checks budget, add to in-memory list and decrease budget and increase capital.
        // Throws InvalidOperationException if budget is insufficient.
        // Optional filePath: if provided the properties file will be rewritten.
        public void PurchaseProperty(Property property, string filePath = null)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (Budget < property.PropertyValue)
                throw new InvalidOperationException("Insufficient budget to purchase the property.");

            Properties.Add(property);
            Capital += property.PropertyValue;
            Budget -= property.PropertyValue;

            if (!string.IsNullOrWhiteSpace(filePath))
                SavePropertiesToFile(filePath);
        }

        // Sell a property: remove from in-memory list, decrease capital and increase budget by 5% of property's value, then persist.
        public void SellProperty(Property property, string filePath = null)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (!Properties.Remove(property))
                return; // nothing removed

            Capital -= property.PropertyValue;
            if (Capital < 0) Capital = 0;

            // Increase budget by 5% of the sold property's value
            double gain = property.PropertyValue * 0.10;
            Budget += gain;

            if (!string.IsNullOrWhiteSpace(filePath))
                SavePropertiesToFile(filePath);
        }

        // Remove from memory and persist change to the given file by rewriting it.
        // Kept for compatibility; it now updates the capital when removal succeeds (but does not change budget).
        public void RemovePropertyAndSave(Property property, string filePath)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required.", nameof(filePath));

            if (!Properties.Remove(property))
                return; // nothing removed

            // adjust capital
            Capital -= property.PropertyValue;
            if (Capital < 0) Capital = 0;

            SavePropertiesToFile(filePath);
        }

        // Collect monthly rent for all rented rentable properties, add to budget and return total collected.
        public double CollectMonthlyRent(string filePath = null)
        {
            double total = 0;
            foreach (var p in Properties)
            {
                if (p is RentableApartment ra && ra.IsRented)
                {
                    total += ra.MonthlyRent;
                }
            }

            if (total > 0)
                Budget += total;

            if (!string.IsNullOrWhiteSpace(filePath))
                SavePropertiesToFile(filePath);

            return total;
        }

        // Collect monthly rent for a single property by address
        public double CollectMonthlyRentFor(string address, string filePath = null)
        {
            var p = findPropertyByAddress(address);
            if (p is RentableApartment ra && ra.IsRented)
            {
                Budget += ra.MonthlyRent;
                if (!string.IsNullOrWhiteSpace(filePath))
                    SavePropertiesToFile(filePath);
                return ra.MonthlyRent;
            }

            return 0;
        }

        // Adjust budget directly (public helper). Use positive amounts to increase, negative to decrease.
        public void AdjustBudget(double amount)
        {
            Budget += amount;
            if (Budget < 0) Budget = 0;
        }

        // Persist the current Properties list to a txt file using the unified schema:
        // Field order (every line, same number of fields):
        // Type, Address, Price, IndoorArea, OutDoorArea, Floor, Bedrooms, HasElevator, IsRented, MonthlyRent, HasGarden
        // Use defaults when a field is not applicable: numeric -> 0, string -> N/A, bool -> false  
        public void SavePropertiesToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required.", nameof(filePath));

            var lines = new List<string>(Properties.Count);
            foreach (var p in Properties)
            {
                string typeToken = "Unknown";
                string address = !string.IsNullOrWhiteSpace(p.Address) ? p.Address : "N/A";
                string price = p.PropertyValue.ToString(CultureInfo.InvariantCulture);
                string indoorArea = "0";
                string outDoorArea = "0";
                string floor = "0";
                string bedrooms = "0";
                string hasElevator = "false";
                string isRented = "false";
                string monthlyRent = "0";
                string hasGarden = "false";

                if (p is RentableApartment ra)
                {
                    typeToken = "RentableApartment";
                    indoorArea = ra.IndoorArea.ToString(CultureInfo.InvariantCulture);
                    floor = ra.Floor.ToString(CultureInfo.InvariantCulture);
                    bedrooms = ra.Bedrooms.ToString(CultureInfo.InvariantCulture);
                    hasElevator = ra.HasElevator.ToString().ToLowerInvariant();
                    isRented = ra.IsRented.ToString().ToLowerInvariant();
                    monthlyRent = ra.MonthlyRent.ToString(CultureInfo.InvariantCulture);
                }
                else if (p is Apartment a)
                {
                    typeToken = "Apartment";
                    indoorArea = a.IndoorArea.ToString(CultureInfo.InvariantCulture);
                    floor = a.Floor.ToString(CultureInfo.InvariantCulture);
                    bedrooms = a.Bedrooms.ToString(CultureInfo.InvariantCulture);
                    hasElevator = a.HasElevator.ToString().ToLowerInvariant();
                }
                else if (p is House h)
                {
                    typeToken = "House";
                    double computedOut = h.OutDoorArea;
                    double computedIn = h.IndoorArea - computedOut;
                    if (computedIn < 0) computedIn = 0;
                    indoorArea = computedIn.ToString(CultureInfo.InvariantCulture);
                    outDoorArea = computedOut.ToString(CultureInfo.InvariantCulture);
                    hasGarden = h.HasGarden.ToString().ToLowerInvariant();
                }
                else
                {
                    typeToken = "UnknownType";
                    indoorArea = p.IndoorArea.ToString(CultureInfo.InvariantCulture);
                }

                lines.Add(string.Join(", ",
                    typeToken,
                    address,
                    price,
                    indoorArea,
                    outDoorArea,
                    floor,
                    bedrooms,
                    hasElevator,
                    isRented,
                    monthlyRent,
                    hasGarden
                ));
            }

            // Write to temp file then move to target to reduce chance of partial writes/locks
            string temp = Path.GetTempFileName();
            System.IO.File.WriteAllLines(temp, lines, System.Text.Encoding.UTF8);
            System.IO.File.Copy(temp, filePath, true);
            System.IO.File.Delete(temp);

            //Console.WriteLine($"[DEBUG] Saved {lines.Count} properties to: {filePath}");
        }

        // Mark property as rented and save file if filePath provided.
        public void RentProperty(string address, string filePath = null)
        {
            var property = Properties.FirstOrDefault(p => p.Address == address);
            if (property is null)
            {
                Console.WriteLine($"No property found at {address}.");
                return;
            }

            if (property is IRentable rentable)
            {
                if (rentable.IsRented)
                {
                    Console.WriteLine($"Property at {address} is already rented.");
                    return;
                }

                rentable.IsRented = true;
                Console.WriteLine($"Property at {address} has been rented.");

                if (!string.IsNullOrWhiteSpace(filePath))
                    SavePropertiesToFile(filePath);

                return;
            }

            Console.WriteLine($"Property at {address} is not rentable.");
        }

        // Mark property as not rented (unrent) and save file if filePath provided.
        public void UnrentProperty(string address, string filePath = null)
        {
            var property = Properties.FirstOrDefault(p => p.Address == address);
            if (property is null)
            {
                Console.WriteLine($"No property found at {address}.");
                return;
            }

            if (property is IRentable rentable)
            {
                if (!rentable.IsRented)
                {
                    Console.WriteLine($"Property at {address} is not currently rented.");
                    return;
                }

                rentable.IsRented = false;
                Console.WriteLine($"Property at {address} is now available (not rented).");

                if (!string.IsNullOrWhiteSpace(filePath))
                    SavePropertiesToFile(filePath);

                return;
            }

            Console.WriteLine($"Property at {address} is not rentable.");
        }

        public void DisplayProperties()
        {
            foreach (var property in Properties)
            {
                Console.WriteLine(property.ToString());
            }
        }
        public List<Property> SearchByPrice(double minPrice, double maxPrice)
        {
            return Properties.Where(p => p.PropertyValue >= minPrice && p.PropertyValue <= maxPrice).ToList();
        }
        public List<Property> SearchByArea(double minArea, double maxArea)
        {
            return Properties.Where(p => p.IndoorArea >= minArea && p.IndoorArea <= maxArea).ToList();
        }
        public void DisplayResults(List<Property> resultProperties)
        {
            int count = resultProperties?.Count ?? 0;
            Console.WriteLine($"{count} properties found.");
            if (count == 0) return;

            foreach (var property in resultProperties)
            {
                Console.WriteLine(property.ToString());
            }
        }
      
        public List<Property> SortBy<TKey>(Func<Property, TKey> keySelector, bool ascending = true)
            where TKey : IComparable<TKey>
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var sorted = new List<Property>(Properties);
            sorted.Sort((a, b) =>
            {
                var ka = keySelector(a);
                var kb = keySelector(b);

                // handle nulls defensively
                if (ka is null && kb is null) return 0;
                if (ka is null) return ascending ? -1 : 1;
                if (kb is null) return ascending ? 1 : -1;

                int cmp = ka.CompareTo(kb);
                return ascending ? cmp : -cmp;
            });

            return sorted;
        }
        // Convenience wrapper that accepts common criterion names (price, area, address).
        public List<Property> SortBy(string criterion, bool ascending = true)
        {
            if (string.IsNullOrWhiteSpace(criterion)) throw new ArgumentException("Criterion required.", nameof(criterion));

            switch (criterion.Trim().ToLowerInvariant())
            {
                case "price":
                case "propertyvalue":
                case "value":
                    return SortBy(p => p.PropertyValue, ascending);
                case "area":
                case "indoorarea":
                    return SortBy(p => p.IndoorArea, ascending);
                case "address":
                    return SortBy(p => p.Address, ascending);
                default:
                    throw new ArgumentException($"Unknown sort criterion: {criterion}", nameof(criterion));
            }
        }


        
    }
}
// sortare cu generics dupa criteriul ales - done 
// citire proprietati din fisier - done
// meniu in main client - done
// printari dupa sortare cu foreach(var property in Properties) - done
// salvare inapoi in fisier dupa stergere - done
// 1. adauga un capital al agentiei(valorea tuturor proprietatilor detinute) si mareste-l 
// cand se cumpara o proprietate si scade-l cand se vinde o proprietate  - done
// 2. adauga un buget care se mareste atunci cand se vinde o proprietate(+5% din valoarea ei)
// sau cand se incaseaza the monthly rent si scade cand se cumpara o proprietate - done
// 3. adauga o noua proprietate - cu introducere de date de la tastatura si salvare in fisier,
// va verifica daca agentia are bugetul necesar pt a o cumpara - done
// 4. introducere oferta de vanzare - acceptare doar daca agentia are bugetul necesar - done
// 5. colectare chirii lunare pentru toate proprietatile inchiriate - done
// adauga submeniuri pentru cautare si sortare 
// adauga max Value pt inputs
// trateaza cazurile in care nu exista proprietati in agentie (ex la colectare chirii, afisare etc) 
// bool - month map ca sa stim cand e colectata chiria
