using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Configuration;

namespace ManifestMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Entrez l'identifiant de la nouvelle version : ");
            string newVersion = Console.ReadLine();

            XmlDocument manifest = new XmlDocument();
            XmlDocument newManifest = new XmlDocument();

            MemoryStream ms = new MemoryStream();
            ms = new MemoryStream(File.ReadAllBytes(ConfigurationManager.AppSettings["manifest"]));
            manifest.Load(ms);

            XmlNodeList currentFiles = manifest.SelectNodes("//fichier");

            //Préparation du nouveau manifest
            XmlDeclaration xmlDeclaration = newManifest.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = newManifest.DocumentElement;
            newManifest.InsertBefore(xmlDeclaration, root);
            XmlElement versionBase = newManifest.CreateElement(string.Empty, "version", string.Empty);
            XmlAttribute id = newManifest.CreateAttribute(string.Empty, "id", string.Empty);
            XmlText idValue = newManifest.CreateTextNode(newVersion);
            id.AppendChild(idValue);
            versionBase.Attributes.Append(id);
            newManifest.AppendChild(versionBase);

            //Indexation des nouveaux fichiers
            foreach (string filePath in Directory.GetFiles(@"New\", "*.*", SearchOption.AllDirectories))
            {
                //Création d'un nouveau fichier
                XmlElement file = newManifest.CreateElement(string.Empty, "fichier", string.Empty);
                versionBase.AppendChild(file);

                //On ajoute le chemin
                XmlElement path = newManifest.CreateElement(string.Empty, "path", string.Empty);
                XmlText pathText = newManifest.CreateTextNode(filePath);
                path.AppendChild(pathText);
                file.AppendChild(path);

                //On ajoute le numéro de version du fichier
                XmlElement version = newManifest.CreateElement(string.Empty, "version", string.Empty);
                string fileVersion = newVersion;

                //On vérifie si le fichier existe dans l'ancienne version
                foreach (XmlNode currentFile in currentFiles)
                {
                    string currentPath = currentFile.SelectSingleNode("path").InnerText;
                    string currentFileVersion = currentFile.SelectSingleNode("version").InnerText;

                    //Si le fichier existe déjà on compare le contenu
                    if (currentPath.Equals(filePath))
                    {
                        DateTime ftime = File.GetLastWriteTime(@"Old\" + filePath);
                        DateTime ftime2 = File.GetLastWriteTime(@"New\" + filePath);

                        //Si le contenu est le même on garde l'ancien numéro de version
                        if (ftime.Equals(ftime2))
                        {
                            fileVersion = currentFileVersion;
                        }
                        break;
                    }
                }

                XmlText versionText = newManifest.CreateTextNode(fileVersion);
                version.AppendChild(versionText);
                file.AppendChild(version);
            }

            newManifest.PreserveWhitespace = true;
            newManifest.Save(ConfigurationManager.AppSettings["manifest"]);
        }
    }
}
