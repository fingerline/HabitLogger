using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

using (var connection = new SqliteConnection("Data Source=habit-tracker.db")){
    
    // Opens database and creates the hydration table if it doesn't exist.
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = "CREATE TABLE IF NOT EXISTS hydration (date TEXT PRIMARY KEY, quantity INTEGER NOT NULL);";
    command.ExecuteNonQuery();
    Console.WriteLine("Connected to database. Table loaded.\n");

    bool exitProgram = false;
    List<string> commands = new List<string> {"i", "d", "u", "v", "exit"};
    List<string> yesno = new List<string> {"y","n"};
    List<string> datequantity = new List<string> {"d", "q"};
    string userMenuSelection = "";

    while(!exitProgram){
        Console.WriteLine("Welcome to the Habit Tracker. Please choose an operation code to perform an action on the database.");
        Console.WriteLine("\ti\t -> Insert a record");
        Console.WriteLine("\td\t -> Delete a record");
        Console.WriteLine("\tu\t -> Update a record");
        Console.WriteLine("\tv\t -> View all records");
        Console.WriteLine("\texit\t -> View all records");
        userMenuSelection = ValidateUserInput(commands.Contains);
        switch(userMenuSelection){
            case "i":
                Console.WriteLine("\nForming insert operation. Please input the date in the form MM/DD/YYYY.");
                string userInputDate = GetDateFromUser();
                Console.WriteLine("\nPlease input the number of times you hydrated.");
                int userInputQuantity = GetQuantityFromUser();
                Console.WriteLine("\nThe record will appear as follows:\n");
                DisplayHeader();
                DisplayRow(userInputDate, userInputQuantity);
                Console.WriteLine("\nIs this correct? (y/n)");
                string userConfirmation = ValidateUserInput(yesno.Contains);
                if(userConfirmation == "n"){
                    Console.WriteLine();
                    break;
                }

                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO hydration VALUES ($date, $quantity);";
                insertCommand.Parameters.AddWithValue("$date", userInputDate);
                insertCommand.Parameters.AddWithValue("$quantity", userInputQuantity);
                try{
                    insertCommand.ExecuteNonQuery();
                } catch (SqliteException e) {
                    Console.WriteLine($"\nCouldn't insert record: {e.Message}\n");
                    break;
                }

                Console.WriteLine("\nRecord Submitted.\n");
                break;
            case "d":
                Console.WriteLine("\nForming delete operation. Please input the date to be deleted in the form MM/DD/YYYY.");
                string userDeleteDate = GetDateFromUser();
                
                // Try and find existing row.
                var selectForDeletion = connection.CreateCommand();
                selectForDeletion.CommandText = "SELECT * FROM hydration WHERE date = $date";
                selectForDeletion.Parameters.AddWithValue("$date", userDeleteDate);

                try{
                    using (var deleteReader = selectForDeletion.ExecuteReader()){
                        //Check if it found anything
                        if(!deleteReader.HasRows){
                            Console.WriteLine("\nCan't find a record on that date.\n");
                            break;
                        }
                        string deleteReadDate = "";
                        int deleteReadQuantity = 0;
                        while(deleteReader.Read()){
                            deleteReadDate = deleteReader.GetString(0);
                            deleteReadQuantity = deleteReader.GetInt32(1);
                        }
                        Console.WriteLine("Record at this day is as follows:\n");
                        DisplayHeader();
                        DisplayRow(deleteReadDate, deleteReadQuantity);
                        Console.WriteLine("\nDo you want to delete this record? (y/n)");
                    }

                } catch (SqliteException e) {
                    Console.WriteLine($"\nError retrieving record: {e.Message}\n");
                    break;
                }

                string deleteConfirmation = ValidateUserInput(yesno.Contains);
                if(deleteConfirmation == "n"){
                    Console.WriteLine();
                    break;
                }
                var deleteCommand = connection.CreateCommand();
                deleteCommand.CommandText = "DELETE FROM hydration WHERE date = $date";
                deleteCommand.Parameters.AddWithValue("$date", userDeleteDate);
                try{
                    deleteCommand.ExecuteNonQuery();
                } catch (SqliteException e) {
                    Console.WriteLine($"\nError deleting record: {e.Message}\n");
                    break;
                }
                Console.WriteLine("\nDeleted record.\n");
                break;
            case "u":
                string changeableDate = "";
                int changeableQuantity = 0;
                string oldDate = "";
                int oldQuantity = 0;
                Console.WriteLine("\nForming update operation. Please input the date of the record you wish to update.");
                string userUpdateDate =  GetDateFromUser();
                var updateSelection = connection.CreateCommand();
                updateSelection.CommandText = "SELECT * FROM hydration WHERE date = $date";
                updateSelection.Parameters.AddWithValue("$date", userUpdateDate);
                try{
                    using(var updateSelectionReader = updateSelection.ExecuteReader()){
                        if(!updateSelectionReader.HasRows){
                            Console.WriteLine("No record by that date!\n");
                            break;
                        }
                        while(updateSelectionReader.Read()){
                            oldDate = updateSelectionReader.GetString(0);
                            oldQuantity = updateSelectionReader.GetInt32(1);
                            changeableDate = oldDate;
                            changeableQuantity = oldQuantity;
                        }
                    }
                } catch (SqliteException e) {
                    Console.WriteLine($"Error obtaining record: {e.Message}\n");
                    break;
                }
                Console.WriteLine("\nHere's what the record looks like now:\n");
                DisplayHeader();
                DisplayRow(oldDate, oldQuantity);
                Console.WriteLine();
                Console.WriteLine("Would you like to update the date, or the quantity? (d/q)");
                string userUpdateField = ValidateUserInput(datequantity.Contains);
                if(userUpdateField == "d"){
                    Console.WriteLine("\nPlease input the new date:");
                    changeableDate = GetDateFromUser();
                } else {
                    Console.WriteLine("\nPlease input the new quantity:");
                    changeableQuantity = GetQuantityFromUser();
                }
                Console.WriteLine($"New record will be as follows:");
                DisplayHeader();
                DisplayRow(changeableDate, changeableQuantity);
                Console.WriteLine();
                Console.WriteLine("Is this correct? (y/n)");
                string userUpdateConfirmation = ValidateUserInput(yesno.Contains);
                if(userUpdateConfirmation == "y"){
                    var userUpdateCommand = connection.CreateCommand();
                    userUpdateCommand.CommandText = "UPDATE hydration SET date = $newdate, quantity = $newquantity WHERE date = $olddate;";
                    userUpdateCommand.Parameters.AddWithValue("$newdate", changeableDate);
                    userUpdateCommand.Parameters.AddWithValue("$newquantity", changeableQuantity);
                    userUpdateCommand.Parameters.AddWithValue("$olddate", oldDate);
                    try{
                        userUpdateCommand.ExecuteNonQuery();
                    } catch (SqliteException e){
                        Console.WriteLine($"\nError updating record: {e.Message}\n");
                        break;
                    }
                    Console.WriteLine("\nRecord updated.\n");
                } else {
                    Console.WriteLine();
                    break;
                }
                break;
            case "v":
                Console.WriteLine("\nCurrent records:");
                DisplayHeader();
                var selectForView = connection.CreateCommand();
                selectForView.CommandText = "SELECT * FROM hydration;";
                try{
                    using(var viewReader = selectForView.ExecuteReader()){
                        while(viewReader.Read()){
                            string viewReadDate = viewReader.GetString(0);
                            int viewReadQuantity = viewReader.GetInt32(1);
                            DisplayRow(viewReadDate, viewReadQuantity);
                        }
                    }
                } catch (SqliteException e) {
                    Console.WriteLine($"Error retrieving database rows: {e.Message}");
                }
                Console.WriteLine();
                break;
            case "exit":
                exitProgram = true;
                break;
            default:
                break;
        }

    }

}

// This is for fun
// Prompts user for input until user supplies input that satisfies tester predicate
string ValidateUserInput(Predicate<string> tester){
    string userInput = "";
    while(true){
        userInput = Console.ReadLine().ToLower();
        if(tester(userInput)){
            return userInput;
        } else {
            Console.WriteLine("Invalid input. Please try again.");
        }
    }
}

string GetDateFromUser(){
    string dateRegex = @"(0[1-9]|1[0-2])\/(0[1-9]|[12][0-9]|3[01])\/(19|20)\d{2}";
    return ValidateUserInput(x => Regex.Match(x, dateRegex).Success);
}

int GetQuantityFromUser(){
    return int.Parse(ValidateUserInput(x => int.TryParse(x, out _)));
}

void DisplayRow(string date, int quantity){
    Console.WriteLine($"{date}\t\t{quantity}");
}

void DisplayHeader(){
    Console.WriteLine("Date\t\t\tQuantity");
    Console.WriteLine("================================");
}