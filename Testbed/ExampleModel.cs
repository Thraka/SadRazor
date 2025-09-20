using System;
using System.Collections.Generic;

namespace Testbed;

public enum DietType { Herbivore, Carnivore, Omnivore }
public enum EnclosureType { Aviary, Aquarium, Savannah, Rainforest, NocturnalHouse, ReptileHouse, PettingZoo }

public record Coordinates(double Latitude, double Longitude);

public class Location
{
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public Coordinates Coordinates { get; init; } = new Coordinates(0,0);
}

public class Zoo
{
    public string Name { get; set; } = "Unnamed Zoo";
    public Location Address { get; set; } = new Location { Name = "Headquarters", City = "", Country = "" , Coordinates = new Coordinates(0,0)};
    public DateTime Established { get; set; } = DateTime.UtcNow;
    public List<Enclosure> Enclosures { get; } = new();
    public List<Caretaker> Staff { get; } = new();

    public static Zoo CreateSample()
    {
        var zoo = new Zoo
        {
            Name = "Evergreen Wildlife Park",
            Established = new DateTime(1994, 5, 12),
            Address = new Location
            {
                Name = "Evergreen Campus",
                City = "Greenville",
                Country = "Utopia",
                Coordinates = new Coordinates(45.1234, -122.9876)
            }
        };

        var alice = new Caretaker { Name = "Alice Green", EmployeeId = 1001, ContactEmail = "alice@evergreen.org", Qualifications = new List<string>{"MSc Zoology","First Aid"} };
        var bob = new Caretaker { Name = "Bob Rivers", EmployeeId = 1002, ContactEmail = "bob@evergreen.org", Qualifications = new List<string>{"Animal Husbandry"} };

        zoo.Staff.Add(alice);
        zoo.Staff.Add(bob);

        var lionSpecies = new Species { CommonName = "African Lion", ScientificName = "Panthera leo", Family = "Felidae", ConservationStatus = "Vulnerable" };
        var giraffeSpecies = new Species { CommonName = "Reticulated Giraffe", ScientificName = "Giraffa camelopardalis reticulata", Family = "Giraffidae", ConservationStatus = "Endangered" };

        var savannah = new Enclosure { Name = "Savannah Plains", Type = EnclosureType.Savannah, AreaSquareMeters = 5000 };
        var simbaMed = new MedicalRecord { LastCheckup = DateTime.UtcNow.AddMonths(-3) };
        simbaMed.Vaccinations.Add("Rabies");
        simbaMed.Notes.Add("Healthy");
        simbaMed.Notes.Add("Minor limp resolved");

        savannah.Animals.Add(new Animal
        {
            Id = 1,
            Name = "Simba",
            Species = lionSpecies,
            DateOfBirth = new DateTime(2015, 6, 1),
            Diet = DietType.Carnivore,
            PrimaryCaretaker = alice,
            Medical = simbaMed
        });

        var giraffeMed = new MedicalRecord { LastCheckup = DateTime.UtcNow.AddMonths(-1) };
        giraffeMed.Vaccinations.Add("Anthrax");
        giraffeMed.Notes.Add("Deworming scheduled");

        var giraffeEnclosure = new Enclosure { Name = "Giraffe Grove", Type = EnclosureType.Savannah, AreaSquareMeters = 2500 };
        giraffeEnclosure.Animals.Add(new Animal
        {
            Id = 2,
            Name = "Tallulah",
            Species = giraffeSpecies,
            DateOfBirth = new DateTime(2018, 9, 15),
            Diet = DietType.Herbivore,
            PrimaryCaretaker = bob,
            Medical = giraffeMed
        });

        savannah.Animals.AddRange(giraffeEnclosure.Animals);

        zoo.Enclosures.Add(savannah);
        zoo.Enclosures.Add(giraffeEnclosure);

        // Feeding schedules
        zoo.Enclosures[0].Animals[0].FeedingSchedules.Add(new FeedingSchedule { Day = DayOfWeek.Monday, Time = new TimeSpan(9, 0, 0), Food = "Beef chunks", QuantityKg = 12.5 });
        zoo.Enclosures[1].Animals[0].FeedingSchedules.Add(new FeedingSchedule { Day = DayOfWeek.Tuesday, Time = new TimeSpan(10, 0, 0), Food = "Acacia browse", QuantityKg = 8.0 });

        return zoo;
    }
}

public class Enclosure
{
    public string Name { get; set; } = string.Empty;
    public EnclosureType Type { get; set; }
    public double AreaSquareMeters { get; set; }
    public List<Animal> Animals { get; } = new();
}

public class Animal
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Species Species { get; set; } = new Species();
    public DateTime DateOfBirth { get; set; }
    public DietType Diet { get; set; }
    public MedicalRecord Medical { get; set; } = new MedicalRecord();
    public List<FeedingSchedule> FeedingSchedules { get; } = new();
    public Caretaker? PrimaryCaretaker { get; set; }

    public string AgeDescription()
    {
        var age = DateTime.UtcNow - DateOfBirth;
        var years = (int)(age.TotalDays / 365.25);
        return years switch
        {
            0 => "less than a year",
            1 => "1 year",
            _ => $"{years} years"
        };
    }
}

public class Species
{
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string ConservationStatus { get; set; } = string.Empty;
}

public class FeedingSchedule
{
    public DayOfWeek Day { get; set; }
    public TimeSpan Time { get; set; }
    public string Food { get; set; } = string.Empty;
    public double QuantityKg { get; set; }
}

public class MedicalRecord
{
    public DateTime LastCheckup { get; set; } = DateTime.UtcNow;
    public List<string> Vaccinations { get; } = new();
    public List<string> Notes { get; } = new();
}

public class Caretaker
{
    public string Name { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public List<string> Qualifications { get; set; } = new();
    public string ContactEmail { get; set; } = string.Empty;
}


