using System;
using System.Xml.Serialization;
using System.Net.Sockets;
using System.IO;

namespace Updater
{
    public class TcpCommunicator<T>
    {
        private NetworkStream clientStream;
        private XmlSerializer xmlSerializer;

        public TcpCommunicator(TcpClient tcpClient)
        {
            this.clientStream = tcpClient.GetStream();
            this.xmlSerializer = new XmlSerializer(typeof(T));
        }

        /// <summary>
        /// Envoi un objet
        /// </summary>
        /// <param name="obj">Objet à envoyer</param>
        public void send(T obj)
        {
            if (this.clientStream.CanWrite)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        this.xmlSerializer.Serialize(ms, obj);

                        byte[] data = ms.ToArray();

                        Byte[] size = BitConverter.GetBytes(data.Length);
                        this.clientStream.Write(size, 0, size.Length);
                        this.clientStream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception)
                {
                    throw new CommunicatorException("Impossible d'envoyer les données");
                }
            }
            else
            {
                throw new CommunicatorException("Flux fermé, envoi impossible");
            }
        }

        /// <summary>
        /// Récupère un objet du type spécifié
        /// </summary>
        /// <returns>Objet</returns>
        public T receive()
        {
            if (this.clientStream.CanRead)
            {
                try
                {
                    Byte[] size = new Byte[sizeof(Int32)];
                    this.clientStream.Read(size, 0, size.Length);

                    Byte[] data = new Byte[BitConverter.ToInt32(size, 0)];
                    this.clientStream.Read(data, 0, data.Length);

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        return (T)this.xmlSerializer.Deserialize(ms);
                    }
                }
                catch (Exception)
                {
                    throw new CommunicatorException("Impossible de lire les données");
                }
            }
            else
            {
                throw new CommunicatorException("Flux fermé, impossible de récupèrer les données");
            }
        }
    }
}
