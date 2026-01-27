

// Refrences : IEvangelist. (2024, June 3). Task-based asynchronous programming - .NET. Microsoft.com. https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    //object used to do the syncronization 
    private static readonly object locker = new object();
    private static volatile bool stopWriting = false;

    static void Main()
    {
        try
        {
            string filepath = @"E:\SRC\WNP\-WNP-A01-Task-1\WDFA01\text.txt";
            string message = "Hello";

            // Run multiple tasks that may throw exceptions
            Task task = Task.WhenAll(
                Task.Run(() => MonnitorFileSize(1000, filepath)),
                //Task.Run(() => Writefile(filepath, message)),
                Task.Run(() => Setnumberofclients(filepath, message, 200))
            );

            // Wait for all tasks to complete 
            task.Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception caught. Processing inner exceptions...\n");

        }
    }

    
    static async Task MonnitorFileSize(int filesize, string filepath)
    {
        for (int i = 0; i < 100; i++)
        {
            // long length = new System.IO.FileInfo(path).Length;
            long length = new FileInfo(filepath).Length;
            Console.WriteLine($"file size: {length}");

            if (length >= filesize)
            {
                Console.WriteLine("File size Reached...");
                stopWriting = true;
                // how to stop async program 
                break;
            }

            // 10 times check per second
            await Task.Delay(100);
        }
    }

    static void Writefile(string filename, string message)
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

    static async Task Setnumberofclients(string filename, string message, int number)
    {
        // code to set number of clients 
        Task[] clientTasks = new Task[number];

        for (int i = 0; i < number; i++)
        {
            int clientId = i + 1; // for logging
            clientTasks[i] = Task.Run(() =>
            {
                if (!stopWriting)
                {
                    Writefile(filename, message);
                }
                    
            });
        }

        await Task.WhenAll(clientTasks);

        Console.WriteLine("All clients have written their messages.");
    }
}
