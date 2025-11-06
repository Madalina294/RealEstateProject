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
                Console.WriteLine("1. Search properties");
                Console.WriteLine("2. Unrent a Property (mark not rented)");
                Console.WriteLine("3. Sell a Property");
                Console.WriteLine("4. Sort properties");
                Console.WriteLine("5. Collect All Monthly Rent");
                Console.WriteLine("6. Purchase / Add Property");
                Console.WriteLine("7. Show Capital & Budget");
                Console.WriteLine("8. Collect Monthly Rent for a Property");
                Console.WriteLine("9. Display current properties");
                Console.WriteLine("10. Rent a Property");
                Console.WriteLine("11. Exit");

                Console.Write("Choose an option (1-11): ");
                string choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1": // Search submenu (unchanged)
                    {
                        // quick check if there are properties
                        var any = agency.SortBy("price", true);
                        if (any == null || any.Count == 0)
                        {
                            Console.WriteLine("No properties available to search.");
                            break;
                        }

                        while (true)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Search submenu:");
                            Console.WriteLine("1. By Price");
                            Console.WriteLine("2. By Area");
                            Console.WriteLine("3. Back");
                            Console.Write("Choose an option (1-3): ");
                            var sub = Console.ReadLine()?.Trim();
                            if (sub == "1")
                            {
                                Console.Write("Enter minimum price: ");
                                if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double minPrice)) { Console.WriteLine("Invalid number."); continue; }
                                Console.Write("Enter maximum price: ");
                                if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double maxPrice)) { Console.WriteLine("Invalid number."); continue; }
                                var priceResults = agency.SearchByPrice(minPrice, maxPrice);
                                agency.DisplayResults(priceResults);
                            }
                            else if (sub == "2")
                            {
                                Console.Write("Enter minimum area: ");
                                if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double minArea)) { Console.WriteLine("Invalid number."); continue; }
                                Console.Write("Enter maximum area: ");
                                if (!double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double maxArea)) { Console.WriteLine("Invalid number."); continue; }
                                var areaResults = agency.SearchByArea(minArea, maxArea);
                                agency.DisplayResults(areaResults);
                            }
                            else if (sub == "3" || string.IsNullOrWhiteSpace(sub))
                            {
                                break; // back to main menu
                            }
                            else
                            {
                                Console.WriteLine("Invalid choice. Try again.");
                            }
                        }
                        break;
                    }

                    case "2":
                    {
                        agency.DisplayProperties();
                        Console.Write("Enter address to mark as not rented (unrent): ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        agency.UnrentProperty(addr, filePath);
                        break;
                    }

                    case "3":
                    {
                        agency.DisplayProperties();
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
                            agency.SellProperty(prop, filePath);
                            Console.WriteLine("Property sold; capital and budget updated and file saved.");
                            LoadPropertiesFromFile(agency, filePath);
                            agency.RecalculateCapital();
                        }
                        break;
                    }

                    case "4": // Sort submenu
                    {
                        var any = agency.SortBy("price", true);
                        if (any == null || any.Count == 0)
                        {
                            Console.WriteLine("No properties available to sort.");
                            break;
                        }

                        while (true)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Sort submenu:");
                            Console.WriteLine("1. By Price");
                            Console.WriteLine("2. By Indoor Area");
                            Console.WriteLine("3. By Address");
                            Console.WriteLine("4. Back");
                            Console.Write("Choose an option (1-4): ");
                            var s = Console.ReadLine()?.Trim();

                            if (s == "1" || s == "2" || s == "3")
                            {
                                Console.Write("Sort ascending? (y/n): ");
                                bool asc = Console.ReadLine()?.Trim().ToLower() == "y";
                                switch (s)
                                {
                                    case "1":
                                        agency.DisplayResults(agency.SortBy("price", asc));
                                        break;
                                    case "2":
                                        agency.DisplayResults(agency.SortBy("area", asc));
                                        break;
                                    case "3":
                                        agency.DisplayResults(agency.SortBy("address", asc));
                                        break;
                                }
                            }
                            else if (s == "4" || string.IsNullOrWhiteSpace(s))
                            {
                                break; // back to main
                            }
                            else
                            {
                                Console.WriteLine("Invalid choice. Try again.");
                            }
                        }
                        break;
                    }

                    case "5":
                    {
                        double collected = agency.CollectMonthlyRent(filePath);
                        Console.WriteLine($"Collected total monthly rent: {collected.ToString(CultureInfo.InvariantCulture)}. Budget updated.");
                        break;
                    }

                    case "6":
                    {
                        try
                        {
                            var newProp = PromptCreatePropertyFromInput();
                            if (newProp == null) { Console.WriteLine("Creation cancelled."); break; }

                            agency.PurchaseProperty(newProp, filePath);
                            Console.WriteLine("Property purchased and saved.");
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

                    case "7":
                    {
                        Console.WriteLine($"Capital: {agency.GetCapital().ToString(CultureInfo.InvariantCulture)}");
                        Console.WriteLine($"Budget: {agency.GetBudget().ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }

                    case "8":
                    {
                        agency.DisplayProperties();
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

                    case "9":
                    {
                        Console.WriteLine("Current Properties:");
                            agency.DisplayProperties();
                            break;
                    }

                case "10":
                    {
                        
                        agency.DisplayProperties();
                        Console.Write("Enter address to rent: ");
                        string addr = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(addr)) { Console.WriteLine("Address required."); break; }

                        agency.RentProperty(addr, filePath);
                        break;
                    }

                    case "11":
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

            // Example ranges — adjust to your domain as needed
            double price = ReadDoubleAllowNA("Price", 0, min: 0, max: 1_000_000_000);
            double indoorArea = ReadDoubleAllowNA("IndoorArea (sqft)", 0, min: 0, max: 100_000);
            double outDoorArea = ReadDoubleAllowNA("OutDoorArea (sqft)", 0, min: 0, max: 100_000);
            int floor = ReadIntAllowNA("Floor", 0, min: 0, max: 100);
            int bedrooms = ReadIntAllowNA("Bedrooms", 0, min: 0, max: 50);
            bool hasElevator = ReadBoolAllowNA("HasElevator (true/false)", false);
            bool isRented = ReadBoolAllowNA("IsRented (true/false)", false);
            double monthlyRent = ReadDoubleAllowNA("MonthlyRent", 0, min: 0, max: 1_000_000);
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

        // Updated: enforces min/max and loops until input is valid or user enters blank/N/A
        private static double ReadDoubleAllowNA(string prompt, double defaultValue, double min, double max)
        {
            if (min > max) throw new ArgumentException("min must be <= max");

            while (true)
            {
                Console.Write($"{prompt} (number between {min} and {max}, or 'N/A' / blank for {defaultValue}): ");
                string s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) return defaultValue;
                if (s.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) return defaultValue;
                if (double.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                {
                    if (v < min || v > max)
                    {
                        Console.WriteLine($"Value out of range [{min}..{max}]. Try again.");
                        continue;
                    }
                    return v;
                }
                Console.WriteLine("Invalid number, try again.");
            }
        }

        // Updated: enforces min/max and loops until input is valid or user enters blank/N/A
        private static int ReadIntAllowNA(string prompt, int defaultValue, int min, int max)
        {
            if (min > max) throw new ArgumentException("min must be <= max");

            while (true)
            {
                Console.Write($"{prompt} (integer between {min} and {max}, or 'N/A' / blank for {defaultValue}): ");
                string s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) return defaultValue;
                if (s.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) return defaultValue;
                if (int.TryParse(s.Trim(), out int v))
                {
                    if (v < min || v > max)
                    {
                        Console.WriteLine($"Value out of range [{min}..{max}]. Try again.");
                        continue;
                    }
                    return v;
                }
                Console.WriteLine("Invalid integer, try again.");
            }
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
