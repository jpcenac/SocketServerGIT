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
                return "NONONONONNONONOOOOOOOOOOOONONOOOOOOOOONOOOOOOOOOO";
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
                try
                {
                    string servControl = string.Empty;

                    //Console.WriteLine("Client Thread started");
                    //SocketSendString(clientSocket, "You are client: " + filePath);
                    //AcceptFileInfo();
                    Console.WriteLine("Control Flow Begins");

                    while (true)
                    {
                        servControl = Data_Receive2(clientSocket);
                        switch (servControl)
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
                                    SocketSendString(clientSocket, "SERVER//Type FileName with extension");
                                    string receiveFile = Data_Receive2(clientSocket);
                                    if (masterDB.Keys.Contains(receiveFile))
                                    {
                                        SocketSendString(clientSocket, "Correct");
                                        clientInfo = CheckFileInfo(receiveFile);
                                        string HostInfo = stringifyTuple(clientInfo);
                                        SocketSendString(clientSocket, HostInfo);

                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Client Entered Incorrect Filename");
                                        SocketSendString(clientSocket, "Incorrect");
                                    }

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);

                                    break;
                                }
                                break;
                            case "Disconnecting":
                                SocketSendString(clientSocket, "SendID");
                                string receiveHostID = Data_Receive2(clientSocket);
                                string[] delAr = receiveHostID.Split(';');
                                string deleteIP = delAr[0];
                                string deletePort = delAr[1];
                                RemoveFileInfo(deleteIP, deletePort);
                                SocketSendString(clientSocket, "RemoveFileInfoSuccess");
                                clientSocket.Close();
                                Console.WriteLine("Client Has Disconnected");
                                Thread.CurrentThread.Abort();
                                break;
                            default:
                                Console.WriteLine("Default Case");
                                //SocketSendString(clientSocket, "In Default Case Currently");
                                break;
                        }
                    }
                }
                catch(Exception e)
                {

                }
            }

            public void AcceptFileInfo()
            {
                
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
                    List<Tuple<string, string, int>> fileInfoList = new List<Tuple<string, string, int>>();
                    Tuple<string, string, int> fileInfo = new Tuple<string, string, int>(filePath, clientIP, clientPort);
                    //Console.WriteLine(commenceCheck);
                    if(!masterDB.ContainsKey(fileName))
                    {
                        fileInfoList.Clear();
                        Console.WriteLine("Adding " + fileName + " to Master Database");

                        Console.WriteLine(fileName + ": " + fileInfo.Item1 + " " + fileInfo.Item2+ " " + fileInfo.Item3.ToString());
                        fileInfoList.Add(fileInfo);
                        masterDB.Add(fileName, fileInfoList);
                    }
                    else
                    {
                        //REDO THIS JANK ASS CODE FUCK YOU
                        //Console.WriteLine(fileName);
                        List<Tuple<string, string, int>> dupTupleList = masterDB[fileName];
                       
                        foreach(Tuple<string, string, int> compTuple in masterDB[fileName].ToList())
                        {
                           
                            if (fileInfo.Item1 == compTuple.Item1 && fileInfo.Item2 == compTuple.Item2 && fileInfo.Item3 == compTuple.Item3)
                            {
                                Console.WriteLine("Duplicate File Found");
                                masterDB[fileName] = masterDB[fileName].Distinct().ToList();
                                
                            }
                            else if (fileInfo.Item1 != compTuple.Item1 && fileInfo.Item2 != compTuple.Item2 || fileInfo.Item3 != compTuple.Item3)
                            {
                                if (fileInfo.Item1 != compTuple.Item1)
                                {
                                    Console.WriteLine("Same Filename, Different Host, Adding");
                                    masterDB[fileName].Add(fileInfo);
                                }
                                else
                                {
                                    Console.WriteLine("Adding new fileInfo to File");
                                    masterDB[fileName].Add(fileInfo);
                                }
                                
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
                foreach(string fn in masterDB.Keys)
                {
                   Console.WriteLine(fn + " :::::::::::: ");
                    foreach(Tuple<string, string, int> tupPrint in masterDB[fn].ToList())
                    {                           
                        Console.WriteLine(tupPrint.Item1 + "\n:::" + tupPrint.Item2 + "\n:::" + tupPrint.Item3);
                       
                        Console.WriteLine();
                    }
                }
            }

            public void SendFileInfo()
            {
                Console.WriteLine("Request for File List Received");
                SocketSendString(clientSocket, "SERVER: SendingFileNames");
                string readyConfirm = Data_Receive2(clientSocket);
                string FileList = string.Empty;
                foreach(string fn in masterDB.Keys)
                {
                    //Console.WriteLine("Sending: " + fn);
                    FileList = FileList + string.Concat(fn, ":");
                    
                }
                Console.WriteLine(FileList);
                SocketSendString(clientSocket, FileList);
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

            public string stringifyTuple(Tuple<string, string, int> cInfo)
            {
                string filePath = cInfo.Item1;
                string ipAddress = cInfo.Item2;
                string portNum = cInfo.Item3.ToString();
                string ipAndPort = String.Concat(ipAddress, ";", portNum);

                string allClietInfo = String.Concat(filePath, ";", ipAndPort);

                return allClietInfo;
            }

            public void RemoveFileInfo(string clientIP, string clientPort)
            {
                int clientPortInt = int.Parse(clientPort);

                foreach(string fileName in masterDB.Keys)
                {
                    foreach(Tuple<string, string, int> targetTuple in masterDB[fileName])
                    {
                        if(targetTuple.Item2 == clientIP && targetTuple.Item3 == clientPortInt)
                        {

                            if(masterDB[fileName].Count > 1)
                            {
                                Console.WriteLine("IP Removed  : " + clientIP);
                                Console.WriteLine("Port Removed: " + clientPort);
                                Console.WriteLine("FileName: " + fileName + "Remains");
                                masterDB[fileName].Remove(targetTuple);
                                RemoveFileInfo(clientIP, clientPort);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("IP Removed  : " + clientIP);
                                Console.WriteLine("Port Removed: " + clientPort);
                                Console.WriteLine("Last Host for File, File Removed");

                                masterDB.Remove(fileName);
                                RemoveFileInfo(clientIP, clientPort);
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                        
                    }

                    break;

                }
            }
            
        }
    }
}
