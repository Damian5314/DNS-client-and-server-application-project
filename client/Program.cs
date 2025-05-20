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

// SendTo();
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
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent)
                              ?? throw new InvalidOperationException("Invalid or missing setting configuration");

    static IPEndPoint serverEndPoint = new IPEndPoint(
        IPAddress.Parse(setting.ServerIPAddress ?? throw new ArgumentNullException("ServerIPAddress is null")),
        setting.ServerPortNumber);

    static IPEndPoint clientEndPoint = new IPEndPoint(
        IPAddress.Parse(setting.ClientIPAddress ?? throw new ArgumentNullException("ClientIPAddress is null")),
        setting.ClientPortNumber);

    static Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    static EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
    static byte[] buffer = new byte[4096];
    static int msgId = 1;

    public static void start()
    {
        InitializeClient();
        SendHelloAndReceiveWelcome();

        List<DNSRecord> dnsQueries = new List<DNSRecord>
        {
            new DNSRecord { Type = "A", Name = "www.outlook.com" },
            new DNSRecord { Type = "MX", Name = "example.com" },
            new DNSRecord { Type = "A", Name = "doesnotexist.com" },
            new DNSRecord { Type = "MX", Name = "" }
        };

        foreach (var query in dnsQueries)
        {
            ProcessDNSQuery(query);
            Console.WriteLine();
        }

        ReceiveAndProcessEndMessage();
    }

    static void InitializeClient()
    {
        clientSocket.Bind(clientEndPoint);
        Console.WriteLine("Client started");
        Console.WriteLine();
    }

    static void SendHelloAndReceiveWelcome()
    {
        var hello = new Message
        {
            MsgId = msgId++,
            MsgType = MessageType.Hello,
            Content = "Hello from client"
        };
        SendMessage(hello);

        var welcome = ReceiveMessage();
        Console.WriteLine("Server To Client: " + welcome.Content);
        Console.WriteLine();
    }

    static void ProcessDNSQuery(DNSRecord query)
    {
        var lookupMsg = new Message
        {
            MsgId = msgId++,
            MsgType = MessageType.DNSLookup,
            Content = query
        };

        SendMessage(lookupMsg);

        var reply = ReceiveMessage();
        if (reply.MsgType == MessageType.DNSLookupReply)
        {
            Console.WriteLine("DNS Reply: " + JsonSerializer.Serialize(reply.Content));
        }
        else if (reply.MsgType == MessageType.Error)
        {
            Console.WriteLine($"Error: {reply.Content} {JsonSerializer.Serialize(query)}");
        }

        SendAcknowledgment(lookupMsg.MsgId);
    }

    static void SendAcknowledgment(int originalMsgId)
    {
        var ack = new Message
        {
            MsgId = msgId++,
            MsgType = MessageType.Ack,
            Content = originalMsgId
        };
        SendMessage(ack);
    }

    static void ReceiveAndProcessEndMessage()
    {
        var end = ReceiveMessage();
        if (end.MsgType == MessageType.End)
        {
            Console.WriteLine("Server To Client: " + end.Content);
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
        return JsonSerializer.Deserialize<Message>(json)
               ?? throw new InvalidOperationException("Failed to deserialize message");
    }
}