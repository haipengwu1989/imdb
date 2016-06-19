using System;
using System.Collections.Generic;

namespace Solution {
    
class Solution {
    /* Entry point to the simple DB system */
    // For the sake of simplicity, assume that all the variables in the DB only takes 32-bit integer values.
    // The basic structure of the program is similar for other data types.
    static void Main(string[] args) {
        var db = new Database();
        var flag = true;
        while(flag){
            var curCommand = Console.ReadLine();
            try{
                // Filter the empty commands
                if(curCommand.Length > 0)
                    // Only END command will set the flag to false and stop the program
                    flag = db.ProcessCommand(curCommand.Split(' '));
            }
            catch(Exception e){
                Console.WriteLine("INVALID COMMAND: " + curCommand);
                throw;
            }
        }
    }
}

/* Class definition for a database object */
public class Database {
    // Store the values of all variables with a dictionary
    private Dictionary<string, int> _varTable;
    // Keep track of the frequency of values in the DB with another dictionary
    private Dictionary<int, uint> _valCount;
    // Use a dictionary to store all the variable changes in a tranaction block.
    // Each variable change is represented by a key-value pair, in which the key is the variable name, 
    // and the value is an array consists of the old value and new value of this variable
    // Organize these blocks with a linked list.
    private LinkedList<Dictionary<string, int?[]>> _blockList;
    
    
    public Database(){
        _varTable = new Dictionary<string, int>();
        _valCount = new Dictionary<int, uint>(); 
        _blockList = new LinkedList<Dictionary<string, int?[]>>();
        _blockList.AddLast(new Dictionary<string, int?[]>());
    }
    
    /* Function to recognize incoming command type and redirect the program */
    public bool ProcessCommand(string[] commandArray){
        switch(commandArray[0]){
            case "GET":
                if(commandArray[1].Length > 0)
                    Get(commandArray[1]);
                else
                    Console.WriteLine("INVALID COMMAND: " + String.Join(" ", commandArray));
                return true;
            case "SET":
                if(commandArray[1].Length > 0)
                    Set(commandArray[1], Int32.Parse(commandArray[2]));
                else
                    Console.WriteLine("INVALID COMMAND: " + String.Join(" ", commandArray));
                return true;
            case "UNSET":
                if(commandArray[1].Length > 0)
                    Unset(commandArray[1]);
                else
                    Console.WriteLine("INVALID COMMAND: " + String.Join(" ", commandArray));
                return true;
            case "NUMEQUALTO":
                if(commandArray[1].Length > 0)
                    NumEqualTo(Int32.Parse(commandArray[1]));
                else
                    Console.WriteLine("INVALID COMMAND: " + String.Join(" ", commandArray));
                return true;
            case "BEGIN":
                Begin();
                return true;
            case "ROLLBACK":
                RollBack();
                return true;
            case "COMMIT":
                Commit();
                return true;
            case "END":
                // If it is an END command, return false to stop the program
                return false;
            default:
                Console.WriteLine("INVALID COMMAND: " + String.Join(" ", commandArray));
                return true;
        }
    }
    
    /* Function to execute GET command. Time complexity is O(1). */
    private void Get(string name){
        if(_varTable.ContainsKey(name))
            // Query the variable table for value
            Console.WriteLine(_varTable[name]);
        else
            Console.WriteLine("NULL");
        }
    
    /* Function to execute SET command. Time complexity is O(1).  */
    private void Set(string name, int val){
        // Retrieve the table of changed variables in most recent transaction block
        var curChangeTable = _blockList.Last.Value;
        if(_varTable.ContainsKey(name)){
            // Store the change in most recent transaction block
            if(curChangeTable.ContainsKey(name))
                curChangeTable[name][1] = val;
            else
                curChangeTable.Add(name, new int?[]{_varTable[name], val});
            // Remove old value from value counter table
            _valCount[_varTable[name]]--;
            if(_valCount[_varTable[name]] == 0)
                _valCount.Remove(_varTable[name]);
            // Update variable table
            _varTable[name] = val;
        }
        else{
            // Store the change in most recentt transaction block
            curChangeTable.Add(name, new int?[]{null, val});
            // Update variable table
            _varTable.Add(name, val);
        }
        
        // Add new value to the value counter
        if(_valCount.ContainsKey(val))
            _valCount[val]++;
        else
             _valCount.Add(val, 1);
    }
    
    /* Function to execute UNSET command. Time complexity is O(1).  */
    private void Unset(string name){
        // To be executed only when the specified variable exists in the variable table
        if(_varTable.ContainsKey(name)){
            // Store the change in transaction block
            var curChangeTable = _blockList.Last.Value;
            if(curChangeTable.ContainsKey(name))
                curChangeTable[name][1] = null;
            else
                curChangeTable.Add(name, new int?[]{_varTable[name], null});
            // Update value counter table
            _valCount[_varTable[name]]--;
            if(_valCount[_varTable[name]] == 0)
                _valCount.Remove(_varTable[name]);
            // Update variable table
            _varTable.Remove(name);
            
        }
    }
    
    /* Function to execute NUMEQUALTO command. Time complexity is O(1).  */
    private void NumEqualTo(int val){
        if(_valCount.ContainsKey(val))
            // Query the value counter table to get the number of occurence
            Console.WriteLine(_valCount[val]);
        else
            Console.WriteLine(0);
        }
    
    /* Function to execute BEGIN command. Time complexity is O(1).  */
    private void Begin(){
        // Start a new transaction block
        _blockList.AddLast(new Dictionary<string, int?[]>());
    }
    
    /* Function to execute ROLLBACK command, time complexity is O(K), where K is the number of variables modified in most recent transaction block  */
    private void RollBack(){
        // Retrieve the list of commands from the most recent block
        var curBlock = _blockList.Last.Value;
        if(curBlock.Count == 0){
            Console.WriteLine("NO TRANSACTION");
        }
        
        // Undo every variable value change in the most recent block
        foreach(var varChange in curBlock){
            // If a variable did not exist prior to this transaction, but was SET during this transaction
            if(!varChange.Value[0].HasValue && varChange.Value[1].HasValue){
                // Reverse value counter table
                _valCount[varChange.Value[1].Value]--;
                if(_valCount[varChange.Value[1].Value] == 0)
                    _valCount.Remove(varChange.Value[1].Value);
                // Reverse variable table
                _varTable.Remove(varChange.Key);
            }
            // If a variable was previously SET, but UNSET during this transaction
            else if(varChange.Value[0].HasValue && !varChange.Value[1].HasValue){
                // Reverse value counter table
                if(_valCount.ContainsKey(varChange.Value[0].Value))
                    _valCount[varChange.Value[0].Value]++;
                else
                    _valCount.Add(varChange.Value[0].Value, 1);
                // Reverse variable table
                _varTable.Add(varChange.Key, varChange.Value[0].Value);
            }
            // If a variable simply changed its value in this transaction
            else if(varChange.Value[0].HasValue && varChange.Value[1].HasValue){
                // Reverse value counter table
                if(_valCount.ContainsKey(varChange.Value[0].Value))
                    _valCount[varChange.Value[0].Value]++;
                else
                    _valCount.Add(varChange.Value[0].Value, 1);
                
                _valCount[varChange.Value[1].Value]--;
                if(_valCount[varChange.Value[1].Value] == 0)
                    _valCount.Remove(varChange.Value[1].Value);
                
                // Reverse variable table
                _varTable[varChange.Key] = varChange.Value[0].Value;
            }
        }
        
        // Remove the most recent block
        _blockList.RemoveLast();
        if(_blockList.Count == 0) _blockList.AddLast(new Dictionary<string, int?[]>());
    }
    
    /* Function to execute COMMIT command. Time complexity is O(T), where T is the number of existing transaction blocks at the time of commit.  */
    private void Commit(){
        // Iterate all the transaction blocks to see if there is any ongoing transaction
        var hasTransaction = false;
        while(_blockList.Count > 0){
            if(_blockList.First.Value.Count > 0) hasTransaction = true;
            _blockList.RemoveFirst();
        }
        
        if(!hasTransaction){
            Console.WriteLine("NO TRANSACTION");
        }
        
        _blockList = new LinkedList<Dictionary<string, int?[]>>();
        _blockList.AddLast(new Dictionary<string, int?[]>());
    }
    
}

}