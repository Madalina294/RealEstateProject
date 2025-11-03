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
            // keep capital in sync after load
            agency.RecalculateCapital();

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
                Console.WriteLine("4. Put a Property up for Rent (unrent)");
                Console.WriteLine("5. Sell a Property");
                Console.WriteLine("6. Sort by Price");
                Console.WriteLine("7. Sort by Indoor Area");
                Console.WriteLine("8. Sort by Address");
                Console.WriteLine("9. Collect All Monthly Rent");
                Console.WriteLine("10. Purchase / Add Property");
                Console.WriteLine("11. Show Capital & Budget");
                Console.WriteLine("12. Collect Monthly Rent for a Property");
                Console.WriteLine("13. Unrent a Property (mark not rented)");
                Console.WriteLine("14. Exit");

                Console.Write("Choose an option (1-14): ");
                string choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                    {
                        Console.Write("Enter minimum price: ");
                        if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double minPrice)) { Console.WriteLine("Invalid number."); break; }
                        Console.Write("Enter maximum price: ");
                        if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double maxPrice)) { Console.WriteLine("Invalid number."); break; }
                        var priceResults = agency.SearchByPrice(minPrice, maxPrice);
                        agency.DisplayResults(priceResults);
                        break;
                    }

                    case "2":
                    {
                        Console.Write("Enter minimum area: ");
                        if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double minArea)) { Console.WriteLine("Invalid number."); break; }
                        Console.Write("Enter maximum area: ");
                        if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double maxArea)) { Console.WriteLine("Invalid number."); break; }
                        var areaResults = agency.SearchByArea(minArea, maxArea);
                        agency.DisplayResults(areaResults);
                        break;
                    }

                    case "3":
                    {
                        Console.Write("Enter property address to rent: ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }
                        agency.RentProperty(addr, filePath);
                        break;
                    }

                    case "4":
                    {
                        Console.Write("Enter property address to be put up for Rent (mark not rented): ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        var prop = agency.findPropertyByAddress(addr);
                        if (prop == null) { Console.WriteLine("Property not found."); break; }

                        if (prop is IRentable rentable)
                        {
                            rentable.IsRented = false;
                            Console.WriteLine("Property is now up for rent (not rented).");
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
                            // use SellProperty to update capital and budget (SellProperty persists if filePath passed)
                            agency.SellProperty(prop, filePath);
                            Console.WriteLine("Property sold; capital and budget updated and file saved.");
                            // reload to ensure in-memory matches file
                            LoadPropertiesFromFile(agency, filePath);
                            agency.RecalculateCapital();
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
                    {
                        double collected = agency.CollectMonthlyRent(filePath);
                        Console.WriteLine($"Collected total monthly rent: {collected.ToString(CultureInfo.InvariantCulture)}. Budget updated.");
                        break;
                    }

                    case "10":
                    {
                        try
                        {
                            var newProp = PromptCreatePropertyFromInput();
                            if (newProp == null) { Console.WriteLine("Creation cancelled."); break; }

                            // attempt purchase (checks budget) - PurchaseProperty will save if filePath provided
                            agency.PurchaseProperty(newProp, filePath);
                            Console.WriteLine("Property purchased and saved.");
                            // reload to reflect saved state
                            LoadPropertiesFromFile(agency, filePath);
                            agency.RecalculateCapital();
                        }
                        catch (InvalidOperationException ioe)
                        {
                            Console.WriteLine($"Cannot purchase: {ioe.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creating/purchasing property: {ex.Message}");
                        }
                        break;
                    }

                    case "11":
                    {
                        Console.WriteLine($"Capital: {agency.GetCapital().ToString(CultureInfo.InvariantCulture)}");
                        Console.WriteLine($"Budget: {agency.GetBudget().ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }

                    case "12":
                    {
                        Console.Write("Enter address to collect monthly rent for: ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        double amt = agency.CollectMonthlyRentFor(addr, filePath);
                        if (amt > 0)
                            Console.WriteLine($"Collected {amt.ToString(CultureInfo.InvariantCulture)} for {addr}. Budget updated.");
                        else
                            Console.WriteLine("No rent collected (property not found, not rentable, or not rented).");
                        break;
                    }

                    case "13":
                    {
                        Console.Write("Enter address to mark as not rented (unrent): ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        agency.UnrentProperty(addr, filePath);
                        break;
                    }

                    case "14":
                        Console.WriteLine("Exiting...");
                        return;

                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }   
        }

        // NEW: Prompt user for every unified-schema field.
        // If the property doesn't have a field, user can enter "N/A" for strings or 0 for numerics.
        // Returns a Property instance or null if canceled.
        private static Property PromptCreatePropertyFromInput()
        {
            Console.WriteLine("Enter property details. For fields not applicable, enter 'N/A' for strings or 0 for numbers.");
            Console.WriteLine("Press Enter to cancel at the type prompt.");

            Console.Write("Type (RentableApartment / Apartment / House) [required]: ");
            string typeInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(typeInput)) return null;
            string normalizedType = typeInput.Trim().ToLowerInvariant();

            string addressInput = ReadStringAllowNA("Address (or N/A)", "N/A");
            // loader treats "N/A" as empty address, so return empty string for N/A
            string address = string.Equals(addressInput, "N/A", StringComparison.OrdinalIgnoreCase) ? string.Empty : addressInput;

            double price = ReadDoubleAllowNA("Price", 0);
            double indoorArea = ReadDoubleAllowNA("IndoorArea", 0);
            double outDoorArea = ReadDoubleAllowNA("OutDoorArea", 0);
            int floor = ReadIntAllowNA("Floor", 0);
            int bedrooms = ReadIntAllowNA("Bedrooms", 0);
            bool hasElevator = ReadBoolAllowNA("HasElevator (true/false)", false);
            bool isRented = ReadBoolAllowNA("IsRented (true/false)", false);
            double monthlyRent = ReadDoubleAllowNA("MonthlyRent", 0);
            bool hasGarden = ReadBoolAllowNA("HasGarden (true/false)", false);

            // If user provided an explicit type use it; otherwise infer
            string resolvedType = normalizedType;
            if (string.IsNullOrWhiteSpace(normalizedType) || normalizedType == "n/a")
            {
                if (hasGarden || outDoorArea > 0) resolvedType = "house";
                else if (isRented || monthlyRent > 0) resolvedType = "rentableapartment";
                else if (floor > 0 || bedrooms > 0) resolvedType = "apartment";
                else resolvedType = "apartment";
            }

            switch (resolvedType)
            {
                case "rentableapartment":
                case "rentable_apartment":
                case "rentable apartment":
                    return new RentableApartment(address, price, indoorArea, floor, bedrooms, hasElevator, isRented, monthlyRent);

                case "apartment":
                    return new Apartment(address, price, indoorArea, floor, bedrooms, hasElevator);

                case "house":
                    return new House(address, price, indoorArea, outDoorArea, floor, hasGarden);

                default:
                    Console.WriteLine("Unknown type. Creation cancelled.");
                    return null;
            }
        }

        // Helpers that accept "N/A" as meaning default (0 for numbers, false for bools).
        private static string ReadStringAllowNA(string prompt, string defaultValue)
        {
            Console.Write($"{prompt} (default {defaultValue}): ");
            string s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            return s.Trim();
        }

        private static double ReadDoubleAllowNA(string prompt, double defaultValue)
        {
            Console.Write($"{prompt} (enter number, or 'N/A' / blank for {defaultValue}): ");
            string s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (s.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) return defaultValue;
            if (double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v)) return v;
            Console.WriteLine("Invalid number, using default.");
            return defaultValue;
        }

        private static int ReadIntAllowNA(string prompt, int defaultValue)
        {
            Console.Write($"{prompt} (enter integer, or 'N/A' / blank for {defaultValue}): ");
            string s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (s.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) return defaultValue;
            if (int.TryParse(s.Trim(), out int v)) return v;
            Console.WriteLine("Invalid integer, using default.");
            return defaultValue;
        }

        private static bool ReadBoolAllowNA(string prompt, bool defaultValue)
        {
            Console.Write($"{prompt} (true/false, or 'N/A' / blank for {defaultValue}): ");
            string s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (s.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) return defaultValue;
            if (bool.TryParse(s.Trim(), out bool v)) return v;
            Console.WriteLine("Invalid boolean, using default.");
            return defaultValue;
        }

        // format: unified schema lines (11 fields)
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
