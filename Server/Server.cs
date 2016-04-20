using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

namespace Server
{
    class Server
    {

        static Socket listenerSocket;
        //filename...filePath, IPAdress, portnumber
        static Dictionary<string, List<Tuple<string, string, int>>> masterDB;

        //start server
        static void Main(string[] args)
        {
            masterDB = new Dictionary<string, List<Tuple<string, string, int>>>();
            Console.Write("Starting server on: ");

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
           
            //IPAddress = System.Net.IPAddress.Parse(Server);
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), 30000);
            listenerSocket.Bind(serverIP);
            Console.WriteLine(serverIP.ToString());

            Thread listenThread = new Thread(ListenThread);

            listenThread.Start();
        }

        //listener: listens for clients
        static void ListenThread()
        {
            while (true)
            {
                listenerSocket.Listen(0);
                ClientData newClient = new ClientData(listenerSocket.Accept());            
            }
        }
       

        public static void SocketSendString(Socket inSock, string input)
        {
            try
            {
                //Console.WriteLine("Sending: " + input);            
                input = input + "<EOF>";
                inSock.Send(Encoding.ASCII.GetBytes(input));
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //with flag
        public static string Data_Receive2(object cSocket)
        {
            string clientData = null;
            Socket clientSocket = (Socket)cSocket;
            byte[] Buffer;
            
            try
            {
                while (true)
                {
                    Buffer = new Byte[1024];
                    int received = clientSocket.Receive(Buffer);
                    // decode data sent
                    clientData += Encoding.ASCII.GetString(Buffer, 0, received);
                    //Console.WriteLine("Attempting Receive");
                    if (clientData.IndexOf("<EOF>") > -1)
                    {
                        //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        //clientData.Length, clientData);
                        break;
                    }

                }
                return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return "SHIEEEEEEEEEEEEET";
            }
            //return clientData.Split(new string[] { "<EOF>" }, StringSplitOptions.None)[0];
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("IP was not found");
        }

        class ClientData
        {
            public Socket clientSocket;
            public Thread clientThread; //
            public string filePath;

            public ClientData(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                filePath = string.Empty;
                clientThread = new Thread(ServerFunction);
                clientThread.Start();
            }

            public void ServerFunction()
            {
                string servControl = string.Empty;

                Console.WriteLine("Client Thread started");
                //SocketSendString(clientSocket, "You are client: " + filePath);
                //AcceptFileInfo();
                Console.WriteLine("Control Flow Begins");

                while(true)
                {
                    servControl = Data_Receive2(clientSocket);
                    switch(servControl)
                    {
                        case "RequestFileList":
                            SendFileInfo();
                            break;

                        case "UpdateFileServer":
                            AcceptFileInfo();
                            break;

                        case "PrintDataBase":
                            CheckDatabase();
                            break;

                        case "DownloadFile":
                            try
                            {
                                Tuple<string, string, int> clientInfo;
                                Console.WriteLine("Server set to Send FileName for Download");
                                SocketSendString(clientSocket, "Type FileName with extension");
                                string receiveFile = Data_Receive2(clientSocket);
                                clientInfo = CheckFileInfo(receiveFile);
                                ////Console.WriteLine("File Owner Info: " + clientInfo.Item1.ToString() 
                                //    + " " + clientInfo.Item2.ToString() + " " 
                                //    + clientInfo.Item3.ToString());

                                Console.WriteLine("Sending FilePath: " + clientInfo.Item1.ToString());
                                SocketSendString(clientSocket, clientInfo.Item1.ToString());

                                Console.WriteLine("Sending IP: " + clientInfo.Item2.ToString());
                                SocketSendString(clientSocket, clientInfo.Item2.ToString());

                                Console.WriteLine("Sending port: " + clientInfo.Item3.ToString());
                                SocketSendString(clientSocket, clientInfo.Item3.ToString());

                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                break;
                            }
                        default:
                            Console.WriteLine("Default Case");
                            //SocketSendString(clientSocket, "In Default Case Currently");
                            break;
                    }
                }
            }

            public void AcceptFileInfo()
            {
                List<Tuple<string, string, int>> fileInfoList = new List<Tuple<string, string, int>>();
                string clientIP = Data_Receive2(clientSocket);
                Console.WriteLine("Receiving File info from: " + clientIP);
                SocketSendString(clientSocket, "SERVER: RECEIVED IP");
                string clientPortString = Data_Receive2(clientSocket);
                Console.WriteLine("ClientPort: " + clientPortString);
                int clientPort = int.Parse(clientPortString);
                SocketSendString(clientSocket, "SERVER: RECEIVED PORT");
                string fileCountString = Data_Receive2(clientSocket);
               // Console.WriteLine(fileCountString);
                int fileCount = int.Parse(fileCountString);
                SocketSendString(clientSocket, "FileCountReceived");
                for(int i = 0; i<fileCount; i++)
                {
                    string fileName = Data_Receive2(clientSocket);
                    //Console.WriteLine(fileName);
                    SocketSendString(clientSocket, "FNReceived");
                    string filePath = Data_Receive2(clientSocket);
                    //Console.WriteLine(filePath);
                    SocketSendString(clientSocket, "FPReceived");
                    string commenceCheck = Data_Receive2(clientSocket);
                    //Console.WriteLine(commenceCheck);
                    if(!masterDB.ContainsKey(fileName))
                    {
                        Console.WriteLine("Adding " + fileName + " to Master Database");

                        Tuple<string, string, int> fileInfo = new Tuple<string, string, int>(filePath, clientIP, clientPort);

                        fileInfoList.Add(fileInfo);
                        masterDB.Add(fileName, fileInfoList);
                    }
                    else
                    {
                        Tuple<string, string, int> fileInfo = new Tuple<string, string, int>(filePath, clientIP, clientPort);
                        //Console.WriteLine(fileName);
                        List<Tuple<string, string, int>> dupTupleList = masterDB[fileName];
                       
                        foreach(Tuple<string, string, int> compTuple in masterDB[fileName].ToList())
                        {
                           
                            if (fileInfo.Item1 == compTuple.Item1 && fileInfo.Item2 == compTuple.Item2 && fileInfo.Item3 == compTuple.Item3)
                            {
                                Console.WriteLine("Duplicate File Found");
                                masterDB[fileName] = masterDB[fileName].Distinct().ToList();
                                
                            }
                            else if (fileInfo.Item1 == compTuple.Item1 && fileInfo.Item2 == compTuple.Item2 && fileInfo.Item3 != compTuple.Item3)
                            {
                                
                                Console.WriteLine("Adding new fileInfo to File");
                                masterDB[fileName].Add(fileInfo);
                            }
                            else
                            {
                                Console.WriteLine("Doing Nothing here");
                            }
                        }
                    }
                }
            }

            public void CheckDatabase()
            {
                for(int i = 0; i < masterDB.Count; i++)
                {
                   Console.WriteLine(i.ToString() + ":::::::::::: ");
                    foreach(List<Tuple<string, string, int>> tupLst in masterDB.Values.Distinct().ToList())
                    {   
                        foreach(Tuple<string, string, int> tupPrint in tupLst)
                        {
                            Console.WriteLine(tupPrint.Item1 + ":::" + tupPrint.Item2 + ":::" + tupPrint.Item3);
                        }
                        Console.WriteLine();
                    }
                }
                
            }

            public void SendFileInfo()
            {
                Console.WriteLine("Request for File List Received");
                SocketSendString(clientSocket, "SERVER: SendingFileNames");
                string readyConfirm = Data_Receive2(clientSocket);
                Console.WriteLine(readyConfirm);
                //string fileCount = masterDB.Count.ToString();
                //Console.WriteLine(fileCount);
                
                foreach(string fn in masterDB.Keys)
                {
                    Console.WriteLine("Sending: " + fn);
                    SocketSendString(clientSocket, fn);
                }
                Console.WriteLine("");
                SocketSendString(clientSocket, "CloseLoop");

            }

            public Tuple<string, string, int> CheckFileInfo(string fileChoice)
            {
                
                Console.WriteLine("FileRequest: " + fileChoice);
            
                //Console.WriteLine(masterDB[fileChoice] == null?"file does not exist" : "file exists in database");
                foreach(Tuple<string, string, int> clientTuple in masterDB[fileChoice])
                {
                    Console.WriteLine("Returning with Valid Client Info");
                    return clientTuple;
                }
                Console.WriteLine("Returning Null");
                return null;
                
            }

            
        }
    }
}
