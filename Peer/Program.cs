﻿using System.Net;
using System.Net.Sockets;
using System.Text;

var successful = Encoding.UTF8.GetBytes("successful");
var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

Console.Write("enter STUN server endpoint: ");
var stunEndPoint = IPEndPoint.Parse(Console.ReadLine() ?? "");

Console.Write("enter port number: ");
var portNumber = Convert.ToInt32(Console.ReadLine());
using var udpClient = new UdpClient(portNumber);

Console.Write("> add ");
var groupName = Console.ReadLine() ?? "default";
var addCommandDataBytes = Encoding.UTF8.GetBytes(groupName);
udpClient.Send(addCommandDataBytes, stunEndPoint);
var addCommandResponseBytes = udpClient.Receive(ref remoteEndPoint);

if (!addCommandResponseBytes.SequenceEqual(successful))
{
    Console.WriteLine("an error occurred while connecting to STUN server");
    return 1;
}
Console.WriteLine("connected to STUN server");

var peerEndPoint = ReadPeerInfo();
Console.WriteLine($"peer endpoint: {peerEndPoint}");

Console.WriteLine("punching UDP hole...");
udpClient.Send(Array.Empty<byte>(), peerEndPoint);

var listenerThread = new Thread(Listen);
listenerThread.Start();

while (true)
{
    Console.Write("> ");
    var message = Console.ReadLine();
    if (!string.IsNullOrEmpty(message))
    {
        var messageDataBytes = Encoding.UTF8.GetBytes(message);
        udpClient.Send(messageDataBytes, peerEndPoint);
    }
}

void Listen()
{
    while (true)
    {
        var messageBytes = udpClient.Receive(ref remoteEndPoint);
        var message = Encoding.UTF8.GetString(messageBytes);

        if (string.IsNullOrEmpty(message)) continue;

        ConsoleHelper.ClearCurrentLine();
        Console.Write($"peer: {message}\n> ");
    }
}

IPEndPoint ReadPeerInfo()
{
    var stunPeerInfoDataBytes = udpClient.Receive(ref remoteEndPoint);
    var dataStringArray = Encoding.UTF8.GetString(stunPeerInfoDataBytes).Split(" ");
    var peerIpAddress = IPAddress.Parse(dataStringArray[0]);
    var peerPort = Convert.ToInt32(dataStringArray[1]);

    return new IPEndPoint(peerIpAddress, peerPort);
}

internal class ConsoleHelper
{
    public static void ClearCurrentLine()
    {
        var currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentLineCursor);
    }
}
