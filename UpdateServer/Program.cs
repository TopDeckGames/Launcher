using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Configuration;

using Updater;

namespace UpdateServer
{
    class Program
    {
        private static TcpListener tcpListener;
        private static Thread listenThread;

        static void Main(string[] args)
        {
            Console.WriteLine("Démarrage");
            tcpListener = new TcpListener(IPAddress.Any, 5555);
            Program.listenThread = new Thread(new ThreadStart(ListenForClients));
            Program.listenThread.Start();
        }

        private static void ListenForClients()
        {
            Program.tcpListener.Start();
            Console.WriteLine("En attente de connexion");

            while (true)
            {
                TcpClient client = Program.tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connecté");
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        private static void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            TcpCommunicator<Message> tcpCommunicator = new TcpCommunicator<Message>(tcpClient);
            
            while (tcpClient.Connected)
            {
                Message message;
                try
                {
                    message = tcpCommunicator.receive();
                    Console.WriteLine("Message reçu");
                }
                catch (CommunicatorException ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }

                Message response = null;
                switch (message.Type)
                {
                    case Updater.Message.MessageType.GetFile:
                        Console.WriteLine("Demande de fichier");
                        response = new Message();
                        response.Type = Message.MessageType.File;
                        if (File.Exists(@"Data\" + message.Data))
                        {
                            response.BinaryData = File.ReadAllBytes(@"Data\" + message.Data);
                        }
                        break;
                    case Updater.Message.MessageType.GetManifest:
                        Console.WriteLine("Demande de manifest");
                        response = new Message();
                        response.Type = Message.MessageType.Manifest;
                        response.BinaryData = File.ReadAllBytes(ConfigurationManager.AppSettings["manifest"]);
                        break;
                    case Updater.Message.MessageType.End:
                        Console.WriteLine("Demande de fin de connexion");
                        tcpClient.Close();
                        break;
                    default:
                        Console.WriteLine("Type de message inconnu");
                        break;
                }

                if (response != null)
                {
                    try
                    {
                        Console.WriteLine("Envoi de la réponse");
                        tcpCommunicator.send(response);
                    }
                    catch (CommunicatorException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            }
        }
    }
}
