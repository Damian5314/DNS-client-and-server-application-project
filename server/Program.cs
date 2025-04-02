using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ServerUDP
{
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // TODO: [Read the JSON file and return the list of DNSRecords]
    static List<DNSRecord> dnsRecords = JsonSerializer.Deserialize<List<DNSRecord>>(File.ReadAllText("DNSrecords.json"));

    static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    static EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);
    static byte[] buffer = new byte[4096];
    static int ackCount = 0;

    public static void start()
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        serverSocket.Bind(serverEP);

        Console.WriteLine("Server is running");

        while (true)
        {
            // TODO:[Receive and print a received Message from the client]
            var incoming = ReceiveMessage();
            Console.WriteLine("\nClient To Server Received: " + incoming.MsgType);

            // Vervangen van switch door if-else
            if (incoming.MsgType == MessageType.Hello)
            {
                var welcome = new Message
                {
                    MsgId = incoming.MsgId + 1,
                    MsgType = MessageType.Welcome,
                    Content = "Welcome from server"
                };
                SendMessage(welcome);
            }
            else if (incoming.MsgType == MessageType.DNSLookup)
            {
                var query = JsonSerializer.Deserialize<DNSRecord>(incoming.Content.ToString());
                Console.WriteLine("DNS Lookup Request:" + query.Name + " (" + query.Type + ")");

                var match = dnsRecords.FirstOrDefault(r => r.Type == query.Type && r.Name == query.Name);

                if (match != null)
                {
                    var response = new Message
                    {
                        MsgId = incoming.MsgId,
                        MsgType = MessageType.DNSLookupReply,
                        Content = match
                    };
                    SendMessage(response);
                }
                else
                {
                    var error = new Message
                    {
                        MsgId = new Random().Next(1000, 9999),
                        MsgType = MessageType.Error,
                        Content = "Domain not found"
                    };
                    SendMessage(error);
                }
            }
            else if (incoming.MsgType == MessageType.Ack)
            {
                Console.WriteLine("ACK message received for MsgId:" + incoming.Content);
                ackCount++;

                if (ackCount == 4)
                {
                    var endMsg = new Message
                    {
                        MsgId = new Random().Next(10000, 99999),
                        MsgType = MessageType.End,
                        Content = "DNS resolution completed"
                    };
                    SendMessage(endMsg);
                }
            }
        }
    }

    static Message ReceiveMessage()
    {
        int len = serverSocket.ReceiveFrom(buffer, ref clientEP);
        string json = Encoding.UTF8.GetString(buffer, 0, len);
        return JsonSerializer.Deserialize<Message>(json);
    }

    static void SendMessage(Message msg)
    {
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.UTF8.GetBytes(json);
        serverSocket.SendTo(data, clientEP);
        Console.WriteLine("Server To Client Sent: " + msg.MsgType);
    }
}