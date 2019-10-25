using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Trajectories;
using System.Runtime.Serialization.Json;

namespace UnityVR
{
    public class TCPSender
    {
        const int PORT_NO = 9900;
        const string SERVER_IP = "127.0.0.1";

        public static void SendMessage(string msgToSend)
        {
            //---create a TCPClient object at the IP and port no.---
            TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
            NetworkStream nwStream = client.GetStream();
            //byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(msgToSend);
            //MessageToUnity msg = new MessageToUnity();
            //msg.Data = ASCIIEncoding.ASCII.GetBytes(msgToSend);
            //byte[] bytesToSend = msg.Data;

            //byte[] bytesToSend = msgToSend.Data;
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(msgToSend);

            //---send the text---
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);

            //while (true)
            //{
            //    msgToSend = Console.ReadLine();
            //    //---send the text---
            //Console.WriteLine("Sending : " + msgToSend);
            //    bytesToSend = ASCIIEncoding.ASCII.GetBytes(msgToSend);
            //    nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            //}

            //---read back the text---
            //byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            //int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            //Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
            //Console.ReadLine();
            client.Close();
        }

        //public static MessageToUnity Serialize(object serializable)
        //{
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        (new BinaryFormatter()).Serialize(memoryStream, serializable);
        //        return new MessageToUnity { Data = memoryStream.ToArray() };
        //    }
        //}
        public static string Serialize(object serializable)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, serializable);
                return memoryStream.ToString();
            }
        }

        //public static object Deserialize(MessageToUnity message)
        //{
        //    using (var memoryStream = new MemoryStream(message.Data))
        //        return (new BinaryFormatter()).Deserialize(memoryStream);
        //}

        public static void SendMovement(Trajectory movement)
        {
            //MessageToUnity message = Serialize(movement);
            //SendMessage(message);
            string serializedJson = WriteFromTrajectory(movement);
            SendMessage(serializedJson);
        }

        public static void SendString(string environment)
        {
            //MessageToUnity message = Serialize(environment);
            //SendMessage(message);
            string message = Serialize(environment);
            SendMessage(message);
        }

        // Create a User object and serialize it to a JSON stream.  
        public static string WriteFromTrajectory(Trajectory trajectory)
        {
            // Create a stream to serialize the object to.  
            var ms = new MemoryStream();

            // Serializer the Trajectory object to the stream.  
            var ser = new DataContractJsonSerializer(typeof(Trajectory));
            ser.WriteObject(ms, trajectory);
            byte[] json = ms.ToArray();
            ms.Close();
            return Encoding.UTF8.GetString(json, 0, json.Length);
        }

        //// Deserialize a JSON stream to a Trajectory object.  
        //public static Trajectory ReadToTrajectory(string json)
        //{
        //    var deserializedUser = new Trajectory();
        //    var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        //    var ser = new DataContractJsonSerializer(deserializedUser.GetType());
        //    deserializedUser = ser.ReadObject(ms) as Trajectory;
        //    ms.Close();
        //    return deserializedUser;
        //}
    }
}
