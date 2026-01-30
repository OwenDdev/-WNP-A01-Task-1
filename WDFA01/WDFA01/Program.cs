

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
using System.Text.RegularExpressions;

class Program
{
    public static async Task Main(string[] args)
    {
        if(args.Length == 1 && args[0] == "/?")
        {
            usageMessage();
            return;
        }

        if (args.Length != 3)
        {
            usageMessage();
            return;
        }

        string ClientNo = args[0];
        string filesize = args[1];
        string Message = args[2];

        string namePattern = @"^[\w\-. ]+$!,:";

        // Validate message
        if (string.IsNullOrWhiteSpace(Message))
        {
            usageMessage();
            return;
        }

        if (Message.Contains("|"))
        {
            usageMessage();
            return;
        }

        if (!Regex.IsMatch(Message, namePattern))
        {
            usageMessage();
            return;
        }

        // Validate size
        if (!int.TryParse(filesize, out int fileSize) || fileSize < 1 || fileSize > 10000000)
        {
            usageMessage();
            return;
        }

        // Validate ClientNo
        if (!int.TryParse(ClientNo, out int threadNum) || threadNum < 1 || threadNum > 10000)
        {
            usageMessage();
            return;
        }

        //string ClientMessage = ClientNo + "|" + filesize + "|" + Message;

        // Create a string for Shutdown command
        string ClientMessage;
        if(Message.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
        {
            ClientMessage = "Shutdown";
        }
        else
        {
            ClientMessage = $"{ClientNo}|{filesize}|{Message}";
        }

        bool done = false;
        Int32 port = int.Parse(ConfigurationManager.AppSettings["Port"]);
        string localAddr = (ConfigurationManager.AppSettings["IP"]);


        using TcpClient client = new TcpClient();
        await client.ConnectAsync(IPAddress.Parse(localAddr), port);

        // Create a NetworkStream
        using NetworkStream stream = client.GetStream();

        //while (!done)
        //{
            // Convert the output stream to a byte array so TCP/IP
            //   can use it
            string message = ClientMessage;
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

            /*
            if (message == "Shutdown")
            {
                done = true;
            }
            */
            
        //}
        Console.WriteLine("Program ended...press any key.");
        Console.ReadKey();
    }

    public static string Menu()
    {
        //string clientNo;
        //string FileSize;
        string Message;
        //string ClientMessage;
        
        //Console.WriteLine("Input Number of clients:");
        //clientNo = Console.ReadLine();
        //Console.WriteLine("Input FileSize:");
        //FileSize = Console.ReadLine();
        Console.WriteLine("Input message");
        Message = Console.ReadLine();

        //ClientMessage = clientNo + "|" + FileSize + "|" + Message;
        //return ClientMessage;
        return Message;
    }

    //
    // FUNCTION : usageMessage
    // DESCRIPTION :
    // This function displays a usage message to the user if assignment defined conditions 
    // PARAMETERS :
    //
    // RETURNS :
    //
    static void usageMessage()
    {
        Console.WriteLine("___Usage Message_____");
        Console.WriteLine("WriteFileMonitor <filesize> <message> ");

        Console.WriteLine();
        Console.WriteLine("Argument 1  <No of CLients>     : Name of the file, Must not be Blank.");
        Console.WriteLine("Argument 2  <filesize>     : Max file size(in bytes) (1 - 10,000,000), Must not be Blank, Must be an Integer.");
        //Console.WriteLine("Argument 3  <threadnum>    : Number of threads to use (1 - 5), Must Not be Blank.");
        Console.WriteLine();

        Console.WriteLine("TcpFileMonitor");
        Console.WriteLine("Modified by Orewen Precious, Najef, Che ping, yi chen");
        Console.WriteLine("Assignment 01 solutionn");
        Console.WriteLine();

        Console.WriteLine("Create Folder nmtemp in C drive if doesnt already exist");
        Console.WriteLine("Port and IP Address Set in Config File");
        Console.WriteLine("Use '/?' to display usage message.");
    }
}
