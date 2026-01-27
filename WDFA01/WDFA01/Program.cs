

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    //object used to do the syncronization 
    private static readonly object locker = new object();
    static void Main()
    {
        try
        {
            // Run multiple tasks that may throw exceptions
            Task task = Task.WhenAll(
                Task.Run(() => MonnitorFileSIze(1000, "C/log.txt")),
                Task.Run(() => Writefile("log.txt", "Hello", 1)),
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

    
    static void MonnitorFileSIze(int filesize, string filepath)
    {
       
    }

    static void Writefile(string filename, string message, int number)
    {
        // code to write to file
         StreamWriter sw = null;

        lock (locker)
        {
            try
            {
                using (sw = new StreamWriter(filename, true))
                {
                    sw.WriteLine(message);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }
    }

    static void Setnumberofclients(int number)
    {
        // code to set number of clients 
    }
}
