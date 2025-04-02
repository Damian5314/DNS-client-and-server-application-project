using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{
    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    static IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
    static IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
    static Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    static EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
    static byte[] buffer = new byte[4096];
    static int msgId = 1;

    public static void start()
    {
        //TODO: [Create endpoints and socket]
        clientSocket.Bind(clientEndPoint);
        Console.WriteLine("Client started");

        //TODO: [Create and send HELLO]
        var hello = new Message
        {
            MsgId = msgId++,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };
        SendMessage(hello);

        //TODO: [Receive and print Welcome from server]
        var welcome = ReceiveMessage();
        Console.WriteLine("Server TO ClientÂ± " + welcome.Content);
        Console.WriteLine();

        // TODO: [Create and send DNSLookup Message]
        List<DNSRecord> dnsQueries = new List<DNSRecord>
        {
            new DNSRecord { Type = "A", Name = "www.outlook.com" },
            new DNSRecord { Type = "MX", Name = "example.com" },
            new DNSRecord { Type = "ppooop", Name = "doesnotexist.com" },
            new DNSRecord { Type = "CNAME", Name = "badrequest.com" }
        };

        foreach (var query in dnsQueries)
        {
            var lookupMsg = new Message
            {
                MsgId = msgId++,
                MsgType = MessageType.DNSLookup,
                Content = query
            };

            SendMessage(lookupMsg);

            //TODO: [Receive and print DNSLookupReply from server]
            var reply = ReceiveMessage();
            if (reply.MsgType == MessageType.DNSLookupReply)
            {
                Console.WriteLine("DNSLookUpReply: " + JsonSerializer.Serialize(reply.Content));
            }
            else if (reply.MsgType == MessageType.Error)
            {
                Console.WriteLine(" Error: " + reply.Content);
            }

            //TODO: [Send Acknowledgment to Server]
            var ack = new Message
            {
                MsgId = msgId++,
                MsgType = MessageType.Ack,
                Content = lookupMsg.MsgId
            };
            SendMessage(ack);
            Console.WriteLine();


            // TODO: [Send next DNSLookup to server]
        }
        //TODO: [Receive and print End from server]
        var end = ReceiveMessage();
        if (end.MsgType == MessageType.End)
        {
            Console.WriteLine("Server To Client:" + end.Content);
            clientSocket.Close();
        }
    }

    static void SendMessage(Message msg)
    {
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.UTF8.GetBytes(json);
        clientSocket.SendTo(data, serverEndPoint);
        Console.WriteLine("Client To Server Sent: " + msg.MsgType);
    }

    static Message ReceiveMessage()
    {
        int len = clientSocket.ReceiveFrom(buffer, ref remoteEP);
        string json = Encoding.UTF8.GetString(buffer, 0, len);
        return JsonSerializer.Deserialize<Message>(json);
    }
}