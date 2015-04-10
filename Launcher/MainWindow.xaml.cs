using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using System.Xml;
using System.IO;
using System.Configuration;
using System.Diagnostics;

using Updater;

namespace Launcher
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private State updateState;
        private State UpdateState
        {
            get { return updateState; }
            set
            {
                updateState = value;
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    this.lblState.Content = Helper.GetDescription<State>(this.updateState)));

                if (value.Equals(State.READY))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        this.cmdPlay.IsEnabled = true));
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += this.Launch;
        }

        public void Launch(object sender, RoutedEventArgs e)
        {
            Thread updateThread = new Thread(new ThreadStart(Update));
            updateThread.Start();
        }

        public void Update()
        {
            this.UpdateState = State.CONNECTING;

            //Connexion au serveur
            TcpClient client = new TcpClient();
            IPEndPoint serverEndPoint = new IPEndPoint(Dns.GetHostEntry("etaverne.ddns.net").AddressList[0], 5555);
            try
            {
                client.Connect(serverEndPoint);
            }
            catch (Exception ex)
            {
                this.UpdateState = State.CONNECTION_ERROR;
                return;
            }

            TcpCommunicator<Message> tcpCommunicator = new TcpCommunicator<Message>(client);

            //Récupèration des information de mise à jour
            this.UpdateState = State.MANIFEST;
            Message message = new Message();
            message.Type = Message.MessageType.GetManifest;
            Message response;
            try
            {
                tcpCommunicator.send(message);
                response = tcpCommunicator.receive();
            }
            catch (CommunicatorException ex)
            {
                this.UpdateState = State.CONNECTION_ERROR;
                client.Close();
                return;
            }

            //Si le serveur a bien renvoyé le manifest
            if(!response.Type.Equals(Message.MessageType.Manifest))
            {
                this.UpdateState = State.CONNECTION_ERROR;
                client.Close();
                return;
            }

            //On charge les manifest
            XmlDocument manifest = new XmlDocument();
            XmlDocument newManifest = new XmlDocument();

            MemoryStream ms = new MemoryStream(response.BinaryData);
            newManifest.Load(ms);
            ms = new MemoryStream(File.ReadAllBytes(ConfigurationManager.AppSettings["manifest"]));
            manifest.Load(ms);

            string currentVersion = manifest.SelectSingleNode("version").Attributes["id"].InnerText;
            string remoteVersion = newManifest.SelectSingleNode("version").Attributes["id"].InnerText;

            //Si la version du serveur est différente de la version locale, on met à jour
            if (!currentVersion.Equals(remoteVersion))
            {
                this.UpdateState = State.GET_FILES;

                //Création d'un dossier temporaire pour stocker les nouveaux fichiers
                if (Directory.Exists(@"Temp"))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(@"Temp");
                    foreach (FileInfo file in directoryInfo.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                    Directory.Delete(@"Temp", true);
                }
                Directory.CreateDirectory(@"Temp");

                XmlNodeList currentFiles = manifest.SelectNodes("//fichier");
                XmlNodeList remoteFiles = newManifest.SelectNodes("//fichier");

                //Liste des fichiers à mettre à jour
                List<string> filesToUpdate = new List<string>();

                //Pour chaque fichier dans le nouveau manifest
                foreach (XmlNode file in remoteFiles)
                {
                    bool isOkay = false;
                    string path = file.SelectSingleNode("path").InnerText;
                    string version = file.SelectSingleNode("version").InnerText;

                    //On vérifie dans le manifest si le fichier existe et si sa version correspond
                    foreach (XmlNode currentFile in currentFiles)
                    {
                        string currentPath = currentFile.SelectSingleNode("path").InnerText;
                        string currentFileVersion = currentFile.SelectSingleNode("version").InnerText;

                        if (currentPath.Equals(path))
                        {
                            isOkay = currentFileVersion.Equals(version);
                            break;
                        }
                    }

                    //Si il le faut, on met le fichier dans la liste des fichiers à mettre à jour
                    if (!isOkay)
                    {
                        filesToUpdate.Add(path);
                    }
                }

                //On récupère les fichiers non à jour
                int i = 0;
                foreach (string path in filesToUpdate)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        this.pgbarLoader.Value = i * 100 / filesToUpdate.Count));
                    i++;

                    //On créée la demande de fichier
                    message = new Message();
                    message.Type = Message.MessageType.GetFile;
                    message.Data = path;
                    response = null;
                    try
                    {
                        tcpCommunicator.send(message);
                        response = tcpCommunicator.receive();
                    }
                    catch (CommunicatorException ex)
                    {
                        this.UpdateState = State.CONNECTION_ERROR;
                        client.Close();
                        return;
                    }

                    //On vérifie le contenu de la réponse
                    if(!response.Type.Equals(Message.MessageType.File) || response.BinaryData == null)
                    {
                        continue;
                    }

                    //Création des répertoires
                    if (!Directory.Exists(@"Temp\" + path)) 
                    {
                        Directory.CreateDirectory(@"Temp\" + path);
                        Directory.Delete(@"Temp\" + path);
                    }

                    //Écriture du fichier
                    using (FileStream fs = new FileStream(@"Temp\" + path, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(response.BinaryData, 0, response.BinaryData.Length);
                    }
                }

                //On copie les nouveaux fichiers dans le répertoire de l'application
                this.UpdateState = State.UPDATE;
                int length = Directory.GetFiles(@"Temp\", "*.*", SearchOption.AllDirectories).Length;
                i = 0;
                foreach (string dirPath in Directory.GetDirectories(@"Temp\", "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(@"Temp\", @""));
                }
                foreach (string newPath in Directory.GetFiles(@"Temp\", "*.*", SearchOption.AllDirectories))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        this.pgbarLoader.Value = i++ * 100 / length));
                    File.Copy(newPath, newPath.Replace(@"Temp\", @""), true);
                }

                //On supprime les fichiers qui n'existent plus dans la nouvelle version
                foreach (XmlNode file in currentFiles)
                {
                    bool exist = false;
                    string path = file.SelectSingleNode("path").InnerText;

                    foreach (XmlNode remoteFile in remoteFiles)
                    {
                        string remotePath = remoteFile.SelectSingleNode("path").InnerText;
                        if (remotePath.Equals(path))
                        {
                            exist = true;
                            break;
                        }
                    }

                    if (!exist)
                    {
                        File.Delete(@"" + path);
                    }
                }
            }

            //On enregistre le nouveau manifest
            this.UpdateState = State.END;
            newManifest.PreserveWhitespace = true;
            newManifest.Save(ConfigurationManager.AppSettings["manifest"]);

            //On supprime le dossier temporaire
            if (Directory.Exists(@"Temp"))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(@"Temp");
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    dir.Delete(true);
                }
                Directory.Delete(@"Temp", true);
            }

            message = new Message();
            message.Type = Message.MessageType.End;
            try
            {
                tcpCommunicator.send(message);
            }
            catch (CommunicatorException)
            {

            }
            client.Close();
            this.UpdateState = State.READY;      
        }

        private void cmdPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(@"HackMyDeck\HackMyDeck.exe");
            }
            catch(Exception ex)
            {
                
            }
        }
    }
}
