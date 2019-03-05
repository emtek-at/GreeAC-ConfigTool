using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigTool
{
    class Program
    {
        static string m_DeviceHost = "";
        static string m_DeviceMac = "";
        static string m_DeviceKey = "";
            
        static void Main(string[] args)
        {
            JObject reqObj = new JObject();
            JObject reqPackObj = new JObject();
            JObject resObj = new JObject();
            JObject resPackObj = new JObject();
            
            while (true)
            {
                Console.Write("AC Device IP or 'exit': ");
                m_DeviceHost = Console.ReadLine().Trim();

                if (m_DeviceHost == "exit")
                    break;


                // scan Device
                reqObj["t"] = "scan";
                string response = sendCommand(reqObj.ToString());
                if (response == "")
                {
                    Console.WriteLine("Device didn't response properly.");
                    continue;
                }
                resObj = JObject.Parse(response);
                resPackObj = JObject.Parse(Crypter.Decrypt((string) resObj["pack"], ""));
                m_DeviceMac = (string) resPackObj["mac"];

                // bind Device
                reqPackObj = new JObject();
                reqPackObj["mac"] = m_DeviceMac;
                reqPackObj["t"] = "bind";
                reqPackObj["uid"] = 0;

                resPackObj = sendCommandPack(reqPackObj, "");
                if ((string) resPackObj["t"] != "bindok")
                    throw new Exception("didn't get 'bindok' message from Device");

                m_DeviceKey = (string) resPackObj["key"];

                
                // print Menue
                string choice = "";
                while (choice != "9")
                {
                    Console.WriteLine("-------------------------");
                    Console.WriteLine("1 - query Device");
                    Console.WriteLine("2 - set Device Name");
                    Console.WriteLine("3 - set Device Remotehost");
                    Console.WriteLine("9 - exit");
                    choice = Console.ReadLine().Trim();
                    switch (choice)
                    {
                        case "1":

                            reqPackObj = new JObject();
                            reqPackObj["cols"] = new JArray("host", "name");
                            reqPackObj["t"] = "status";
                            reqPackObj["mac"] = 0;
                            resPackObj = sendCommandPack(reqPackObj, m_DeviceKey);

                            Console.WriteLine("Device Name: " + (string) resPackObj["dat"][1]);
                            Console.WriteLine("Device Remotehost: " + (string) resPackObj["dat"][0]);
                            break;
                        case "2":
                            Console.Write("new Device Name: ");
                            string newDeviceName = Console.ReadLine();

                            reqPackObj = new JObject();
                            reqPackObj["opt"] = new JArray("name");
                            reqPackObj["p"] = new JArray(newDeviceName);
                            reqPackObj["t"] = "cmd";
                            resPackObj = sendCommandPack(reqPackObj, m_DeviceKey);
                            break;
                        case "3":
                            Console.Write("new Device Remotehost: ");
                            string newDeviceRemoteHost = Console.ReadLine();

                            reqPackObj = new JObject();
                            reqPackObj["opt"] = new JArray("host");
                            reqPackObj["p"] = new JArray(newDeviceRemoteHost);
                            reqPackObj["t"] = "cmd";
                            resPackObj = sendCommandPack(reqPackObj, m_DeviceKey);
                            break;
                    }
                }
            }
        }

        private static JObject sendCommandPack(JObject pack, string key)
        {
            JObject reqObj = new JObject();
            reqObj["cid"] = "app";
            reqObj["i"] = key == "" ? 1 : 0;
            reqObj["pack"] = Crypter.Encrypt(pack.ToString(), key);
            reqObj["t"] = "pack";
            reqObj["tcid"] = m_DeviceMac;
            reqObj["uid"] = 22130;
            
            string response = sendCommand(reqObj.ToString());
            JObject resObj = JObject.Parse(response);
            return JObject.Parse(Crypter.Decrypt((string)resObj["pack"], key));
        }
        private static string sendCommand(string Command)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            string returnData = "";
            UdpClient udpClient = null;

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    udpClient = new UdpClient(RemoteIpEndPoint);
                    udpClient.Client.SendTimeout = 1000;
                    udpClient.Client.ReceiveTimeout = 1000;
                    udpClient.Connect(m_DeviceHost, 7000);

                    // Sends a message to the host to which you have connected.
                    Byte[] sendBytes = Encoding.ASCII.GetBytes(Command);

                    udpClient.Send(sendBytes, sendBytes.Length);
                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);

                    returnData = Encoding.ASCII.GetString(receiveBytes);
                    
                    udpClient.Close();

                    break;
                }
                catch (Exception e)
                {
                    try
                    {
                        udpClient.Close();
                    }
                    catch { }

                    returnData = "";
                }
            }

            return returnData;
        }
    }
}