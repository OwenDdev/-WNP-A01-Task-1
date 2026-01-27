

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        try
        {
            // Run multiple tasks that may throw exceptions
            Task task = Task.WhenAll(
                Task.Run(() => MonnitorFileSIze(1000)),
                Task.Run(() => Writetofile("log.txt", "Hello", 1)),
                Task.Run(() => Setnumberofclients(200))
            );

            // Wait for all tasks to complete (exceptions will be aggregated)
            task.Wait();
        }
        catch (Exception Ex)
        {
            Console.WriteLine("Exception caught. Processing inner exceptions...\n");

        }
    }

    // Example methods that throw exceptions
    static void MonnitorFileSIze(int FileSize)
    {
        // code to monitor file size
    }

    static void Writetofile(string FileName, string message, int number)
    {
        // code to monitor file size
    }

    static void Setnumberofclients(int number)
    {
        // code to set number of clients 
    }
}
