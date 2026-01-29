

// Refrences : IEvangelist. (2024, June 3). Task-based asynchronous programming - .NET. Microsoft.com. https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.ComponentModel.Design;
using System.Net;
using System.Configuration;

class Program
{
    public static async Task Main()
    {
        bool done = false;
        Int32 port = int.Parse(ConfigurationManager.AppSettings["Port"]);
        string localAddr = (ConfigurationManager.AppSettings["IP"]);


        using TcpClient client = new TcpClient();
        await client.ConnectAsync(IPAddress.Parse(localAddr), port);

        // Create a NetworkStream
        using NetworkStream stream = client.GetStream();

        while (!done)
        {
            // Convert the output stream to a byte array so TCP/IP
            //   can use it
            string message = Menu();
            byte[] data = Encoding.UTF8.GetBytes(message);

            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine("Message sent");

            // We need an input buffer.
            // Then we can read the data.
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            // The input data is in bytes so we must convert it to
            //    a string to use it.
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Server replied: {response}");

            if (message == "Shutdown")
            {
                done = true;
            }
            
        }
        Console.WriteLine("Program ended...press any key.");
        Console.ReadKey();
    }

    public static string Menu()
    {
        string clientNo;
        string FileSize;
        string Message;
        string ClientMessage;
        
        Console.WriteLine("Input Number of clients:");
        clientNo = Console.ReadLine();
        Console.WriteLine("Input FileSize:");
        FileSize = Console.ReadLine();
        Console.WriteLine("Input message");
        Message = Console.ReadLine();

        ClientMessage = clientNo + "|" + FileSize + "|" + Message;
        return ClientMessage;

    }
}
