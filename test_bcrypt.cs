using System;
public class Program {
    public static void Main() {
        bool match = BCrypt.Net.BCrypt.Verify("Welcome@123", "$2a$11$Sy8Lv95X3Ih9zdeqIfvjMOk2Ig31ZWfVlLCswgbEVg0g4uprC/c2O");
        Console.WriteLine(match);
        bool match2 = BCrypt.Net.BCrypt.Verify("Tango!@12345", "$2a$11$Sy8Lv95X3Ih9zdeqIfvjMOk2Ig31ZWfVlLCswgbEVg0g4uprC/c2O");
        Console.WriteLine(match2);
    }
}
