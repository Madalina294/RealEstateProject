using RealEstate;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
namespace RealEstate
{
    public class MainClient 
    {
        public static void Main(string[] args)
        {
            RealEstateAgency agency = new RealEstateAgency();

            // Prefer the output-folder filename and allow an environment override.
            string fileName = "properties.txt";
            string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

            var env = Environment.GetEnvironmentVariable("PROPERTIES_FILE");
            if (!string.IsNullOrWhiteSpace(env))
                filePath = env;

            #if DEBUG
                Console.WriteLine($"Looking for: {filePath}");
                Console.WriteLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
            #endif

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Properties file not found: {filePath}");
                Console.WriteLine("Fixes: place file next to the exe or set the PROPERTIES_FILE environment variable.");
                return;
            }   

            LoadPropertiesFromFile(agency, filePath);
            agency.DisplayProperties();

            // pass filePath so menu actions can persist changes
            showMenu(agency, filePath);
        }

        // note: signature changed to accept the file path used to load/save
        public static void showMenu(RealEstateAgency agency, string filePath)
        {
            if (agency == null) throw new ArgumentNullException(nameof(agency));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required.", nameof(filePath));

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Menu:");
                Console.WriteLine("1. Search by Price");
                Console.WriteLine("2. Search by Area");
                Console.WriteLine("3. Rent a Property");
                Console.WriteLine("4. Put a Property up for Rent");
                Console.WriteLine("5. Sell a Property");
                Console.WriteLine("6. Sort by Price");
                Console.WriteLine("7. Sort by Indoor Area");
                Console.WriteLine("8. Sort by Address");
                Console.WriteLine("9. Exit");

                Console.Write("Choose an option (1-9): ");
                string choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                    {
                        Console.Write("Enter minimum price: ");
                        if (!double.TryParse(Console.ReadLine(), out double minPrice)) { Console.WriteLine("Invalid number."); break; }
                        Console.Write("Enter maximum price: ");
                        if (!double.TryParse(Console.ReadLine(), out double maxPrice)) { Console.WriteLine("Invalid number."); break; }
                        var priceResults = agency.SearchByPrice(minPrice, maxPrice);
                        agency.DisplayResults(priceResults);
                        break;
                    }

                    case "2":
                    {
                        Console.Write("Enter minimum area: ");
                        if (!double.TryParse(Console.ReadLine(), out double minArea)) { Console.WriteLine("Invalid number."); break; }
                        Console.Write("Enter maximum area: ");
                        if (!double.TryParse(Console.ReadLine(), out double maxArea)) { Console.WriteLine("Invalid number."); break; }
                        var areaResults = agency.SearchByArea(minArea, maxArea);
                        agency.DisplayResults(areaResults);
                        break;
                    }

                    case "3":
                    {
                        Console.Write("Enter property address to rent: ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }
                        agency.RentProperty(addr);
                        break;
                    }

                    case "4":
                    {
                        Console.Write("Enter property address to be put up for Rent: ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        var prop = agency.findPropertyByAddress(addr);
                        if (prop == null) { Console.WriteLine("Property not found."); break; }

                        if (prop is IRentable rentable)
                        {
                            rentable.IsRented = false;
                            Console.WriteLine("Property is now up for rent.");
                            // optionally persist this change:
                            agency.SavePropertiesToFile(filePath);
                        }
                        else
                        {
                            Console.WriteLine("Property is not rentable.");
                        }
                        break;
                    }

                    case "5":
                    {
                        Console.Write("Enter property address to sell: ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        var prop = agency.findPropertyByAddress(addr);
                        if (prop == null)
                        {
                            Console.WriteLine("Property not found.");
                        }
                        else
                        {
                            // remove and persist immediately
                            agency.RemovePropertyAndSave(prop, filePath);
                            Console.WriteLine("Property removed and file updated.");
                            LoadPropertiesFromFile(agency, filePath); // reload to reflect changes
                            }
                        break;
                    }

                    case "6":
                    {
                        Console.Write("Sort ascending by Price? (y/n): ");
                        bool ascPrice = Console.ReadLine()?.Trim().ToLower() == "y";
                        var sortedByPrice = agency.SortBy("price", ascPrice);
                        agency.DisplayResults(sortedByPrice);
                        break;
                    }

                    case "7":
                    {
                        Console.Write("Sort ascending by Indoor Area? (y/n): ");
                        bool ascIndoorArea = Console.ReadLine()?.Trim().ToLower() == "y";
                        var sortedByIndoorArea = agency.SortBy("area", ascIndoorArea);
                        agency.DisplayResults(sortedByIndoorArea);
                        break;
                    }

                    case "8":
                    {
                        Console.Write("Sort ascending by Address? (y/n): ");
                        bool ascAddress = Console.ReadLine()?.Trim().ToLower() == "y";
                        var sortedByAddress = agency.SortBy("address", ascAddress);
                        agency.DisplayResults(sortedByAddress);
                        break;
                    }

                    case "9":
                        Console.WriteLine("Exiting...");
                        return;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        // format: first field = type token (RentableApartment/Apartment/House), then fields as documented above.
        public static void LoadPropertiesFromFile(RealEstateAgency agency, string filePath)
        {
            if (agency == null) throw new ArgumentNullException(nameof(agency));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Properties file not found.", filePath);
            
            // Clear existing properties before loading new ones so the method can be reused safely (case 5 etc.)
            agency.RemoveAllProperties();

            int lineNo = 0;
            foreach (var rawLine in File.ReadLines(filePath))
            {
                lineNo++;
                var line = rawLine?.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                var parts = line.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 11)
                {
                    Console.WriteLine($"Line {lineNo}: expected 11 fields, got {parts.Length} - skipped.");
                    continue;
                }
                    
                // Fields: Type, Address, Price, IndoorArea, OutDoorArea, Floor, Bedrooms, HasElevator, IsRented, MonthlyRent, HasGarden
                string typeField = parts[0];
                string address = parts[1] == "N/A" ? string.Empty : parts[1];
                double price = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var tmpD) ? tmpD : 0;
                double indoorArea = double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out tmpD) ? tmpD : 0;
                double outDoorArea = double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out tmpD) ? tmpD : 0;
                int floor = int.TryParse(parts[5], out var tmpI) ? tmpI : 0;
                int bedrooms = int.TryParse(parts[6], out tmpI) ? tmpI : 0;
                bool hasElevator = bool.TryParse(parts[7], out var tmpB) ? tmpB : false;
                bool isRented = bool.TryParse(parts[8], out tmpB) ? tmpB : false;
                double monthlyRent = double.TryParse(parts[9], NumberStyles.Any, CultureInfo.InvariantCulture, out tmpD) ? tmpD : 0;
                bool hasGarden = bool.TryParse(parts[10], out tmpB) ? tmpB : false;

                string normalized = (typeField ?? string.Empty).Trim().ToLowerInvariant();
                string resolvedType = normalized;
                if (string.IsNullOrEmpty(normalized) || normalized == "n/a")
                {
                    // infer
                    if (hasGarden || outDoorArea > 0) resolvedType = "house";
                    else if (isRented || monthlyRent > 0) resolvedType = "rentableapartment";
                    else if (floor > 0 || bedrooms > 0) resolvedType = "apartment";
                    else resolvedType = "apartment";
                }

                try
                {
                    switch (resolvedType)
                    {
                        case "rentableapartment":
                        case "rentable_apartment":
                        case "rentable apartment":
                            agency.AddProperty(new RentableApartment(
                                address, price, indoorArea, floor, bedrooms, hasElevator, isRented, monthlyRent));
                            break;

                        case "apartment":
                            agency.AddProperty(new Apartment(address, price, indoorArea, floor, bedrooms, hasElevator));
                            break;

                        case "house":
                            agency.AddProperty(new House(address, price, indoorArea, outDoorArea, floor, hasGarden));
                            break;

                        default:
                            Console.WriteLine($"Line {lineNo}: unknown/resolved type '{resolvedType}' - skipped.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Line {lineNo}: error creating property - {ex.Message}");
                }
            }
        }
    }
}
